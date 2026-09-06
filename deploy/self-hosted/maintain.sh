#!/usr/bin/env bash
# =============================================================================
# OpenWish maintenance script
#
# Point it at a private environment file:
#
#   OPENWISH_ENV_FILE=/opt/openwish/.env ./maintain.sh
#
# With no command, the script backs up, pulls, deploys, health-checks, and
# rolls back the application image if the new deployment is unhealthy.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="${OPENWISH_ENV_FILE:-${SCRIPT_DIR}/.env}"

if [ -f "$ENV_FILE" ]; then
    set -a
    # shellcheck disable=SC1090
    source "$ENV_FILE"
    set +a
else
    echo "ERROR: No .env file found at ${ENV_FILE}"
    echo "Set OPENWISH_ENV_FILE or place a .env next to this script."
    echo "See .env.example for reference."
    exit 1
fi

for var in OPENWISH_SOURCE_DIR OPENWISH_DEPLOY_DIR OPENWISH_BACKUP_DIR OPENWISH_DB_PASSWORD OPENWISH_BASE_URI; do
    [ -n "${!var:-}" ] || {
        echo "ERROR: $var must be set in ${ENV_FILE}"
        exit 1
    }
done

BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"
HEALTH_URL="${HEALTH_URL:-http://127.0.0.1:${OPENWISH_PORT:-5001}/}"
HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-120}"
HEALTH_INTERVAL="${HEALTH_INTERVAL:-5}"
COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-openwish}"
OPENWISH_IMAGE="${OPENWISH_IMAGE:-ghcr.io/mitch-b/openwish-web:latest}"
POSTGRES_IMAGE="${POSTGRES_IMAGE:-postgres:18}"

COMPOSE_SOURCE="${OPENWISH_SOURCE_DIR}/deploy/self-hosted/compose.yml"
COMPOSE_FILE="${OPENWISH_DEPLOY_DIR}/compose.yml"
LOG_FILE="${OPENWISH_DEPLOY_DIR}/error.log"

log() {
    echo "[$(date -u '+%Y-%m-%dT%H:%M:%SZ')] $*"
}

err() {
    echo "[$(date -u '+%Y-%m-%dT%H:%M:%SZ')] ERROR: $*" | tee -a "$LOG_FILE" >&2
}

fail() {
    err "$*"
    exit 1
}

compose() {
    docker compose -p "$COMPOSE_PROJECT_NAME" -f "$COMPOSE_FILE" --env-file "$ENV_FILE" "$@"
}

require_command() {
    command -v "$1" >/dev/null 2>&1 || fail "Required command not found: $1"
}

SOURCE_OWNER="$(stat -c '%U' "$OPENWISH_SOURCE_DIR")"
SOURCE_HOME="$(getent passwd "$SOURCE_OWNER" | cut -d: -f6)"

if [ "$EUID" -eq 0 ] && [ -z "${DOCKER_CONFIG:-}" ] && [ -f "${SOURCE_HOME}/.docker/config.json" ]; then
    export DOCKER_CONFIG="${SOURCE_HOME}/.docker"
fi

git_repo() {
    if [ "$EUID" -eq 0 ] && [ "$SOURCE_OWNER" != "root" ]; then
        runuser -u "$SOURCE_OWNER" -- git -C "$OPENWISH_SOURCE_DIR" "$@"
    else
        git -C "$OPENWISH_SOURCE_DIR" "$@"
    fi
}

database_is_running() {
    [ -f "$COMPOSE_FILE" ] && [ -n "$(compose ps --status running --quiet db 2>/dev/null)" ]
}

sync_compose() {
    [ -f "$COMPOSE_SOURCE" ] || fail "Compose source not found: $COMPOSE_SOURCE"
    mkdir -p "$OPENWISH_DEPLOY_DIR"
    cp "$COMPOSE_SOURCE" "$COMPOSE_FILE"
}

backup() {
    local timestamp backup_name staging
    timestamp="$(date -u +%Y%m%d%H%M%S)-${BASHPID}"
    backup_name="openwish-backup-${timestamp}.tar.gz"
    staging="${OPENWISH_BACKUP_DIR}/.staging-${timestamp}"

    log "=== Backup ==="
    mkdir -p "$OPENWISH_BACKUP_DIR" "$staging"
    chmod 700 "$OPENWISH_BACKUP_DIR" "$staging"

    if database_is_running; then
        log "Dumping PostgreSQL..."
        # shellcheck disable=SC2016 # Expand PostgreSQL variables inside the container.
        if compose exec -T db sh -c \
            'pg_dump --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --format=custom --no-owner --no-privileges' \
            > "${staging}/postgres.pgdump"; then
            chmod 600 "${staging}/postgres.pgdump"
            log "PostgreSQL dump complete."
        else
            rm -rf "$staging"
            fail "pg_dump failed."
        fi
    else
        log "PostgreSQL is not running; skipping backup."
    fi

    if [ -n "$(ls -A "$staging" 2>/dev/null)" ]; then
        tar -czf "${OPENWISH_BACKUP_DIR}/${backup_name}" -C "$staging" .
        chmod 600 "${OPENWISH_BACKUP_DIR}/${backup_name}"
        log "Backup complete: ${backup_name}"
    else
        log "Nothing to back up."
    fi
    rm -rf "$staging"
}

prune_backups() {
    [[ "$BACKUP_RETENTION_DAYS" =~ ^[0-9]+$ ]] ||
        fail "BACKUP_RETENTION_DAYS must be a non-negative integer."

    log "=== Pruning backups older than ${BACKUP_RETENTION_DAYS} days ==="
    find "$OPENWISH_BACKUP_DIR" -maxdepth 1 -type f \
        -name 'openwish-backup-*.tar.gz' -mtime "+${BACKUP_RETENTION_DAYS}" -print -delete
}

wait_for_health() {
    local elapsed=0 http_code

    log "=== Healthcheck (timeout=${HEALTH_TIMEOUT}s url=${HEALTH_URL}) ==="
    while [ "$elapsed" -lt "$HEALTH_TIMEOUT" ]; do
        http_code="$(curl -sS -o /dev/null -w '%{http_code}' "$HEALTH_URL" 2>/dev/null || true)"
        if [[ "$http_code" =~ ^(2|3)[0-9][0-9]$ ]]; then
            log "Services healthy (HTTP ${http_code})."
            return 0
        fi
        log "Waiting... (${elapsed}s elapsed, last HTTP ${http_code:-000})"
        sleep "$HEALTH_INTERVAL"
        elapsed=$((elapsed + HEALTH_INTERVAL))
    done
    return 1
}

update() {
    local old_commit old_image old_postgres_image old_compose
    old_compose="${OPENWISH_DEPLOY_DIR}/compose.yml.previous"

    log "=== Step 1: Backup ==="
    backup

    log "=== Step 2: Prune old backups ==="
    prune_backups

    log "=== Step 3: Git pull ==="
    old_commit="$(git_repo rev-parse HEAD)"
    git_repo pull --ff-only

    log "=== Step 4: Sync Compose configuration ==="
    mkdir -p "$OPENWISH_DEPLOY_DIR"
    if [ -f "$COMPOSE_FILE" ]; then
        cp "$COMPOSE_FILE" "$old_compose"
    else
        rm -f "$old_compose"
    fi
    sync_compose

    log "=== Step 5: Record current image for rollback ==="
    old_image="$(docker image inspect --format='{{.Id}}' "$OPENWISH_IMAGE" 2>/dev/null || true)"
    old_postgres_image="$(docker image inspect --format='{{.Id}}' "$POSTGRES_IMAGE" 2>/dev/null || true)"

    rollback() {
        err "=== ROLLBACK: restoring previous application image ==="
        if [ -z "$old_image" ]; then
            err "No previous OpenWish image was available; automatic rollback is not possible."
            return 1
        fi
        if [ -f "$old_compose" ]; then
            cp "$old_compose" "$COMPOSE_FILE"
        else
            git_repo show "${old_commit}:deploy/self-hosted/compose.yml" > "$COMPOSE_FILE"
        fi
        docker tag "$old_image" "$OPENWISH_IMAGE"
        if [ -n "$old_postgres_image" ]; then
            docker tag "$old_postgres_image" "$POSTGRES_IMAGE"
        fi
        compose up -d --remove-orphans --no-pull 2>&1 | tee -a "$LOG_FILE"
        err "Rollback complete. The prior application image is running."
    }

    log "=== Step 6: Pull images and deploy ==="
    compose pull
    compose up -d --remove-orphans

    log "=== Step 7: Healthcheck ==="
    if wait_for_health; then
        rm -f "$old_compose"
        log "=== Deploy complete ==="
    else
        err "Services did not become healthy after commit $(git_repo rev-parse --short HEAD)."
        rollback
        exit 1
    fi
}

restore() {
    local backup_file="${1:-}"

    [ -n "$backup_file" ] || fail "Usage: $0 restore PATH_TO_BACKUP"
    [ -r "$backup_file" ] || fail "Backup is not readable: $backup_file"
    database_is_running || fail "PostgreSQL is not running."
    tar -tzf "$backup_file" | grep -qx './postgres.pgdump' ||
        fail "Backup does not contain postgres.pgdump."

    echo "This replaces the current OpenWish database with ${backup_file}."
    read -r -p "Type RESTORE to continue: " confirmation
    [ "$confirmation" = "RESTORE" ] || fail "Restore cancelled."

    backup
    compose stop web
    # shellcheck disable=SC2016 # Expand PostgreSQL variables inside the container.
    if ! tar -xOzf "$backup_file" ./postgres.pgdump |
        compose exec -T db sh -c \
            'pg_restore --clean --if-exists --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --no-owner --no-privileges'; then
        compose up -d web
        fail "Database restore failed; the web service was restarted."
    fi
    compose up -d web
    wait_for_health || fail "Restore completed, but OpenWish is not healthy."
    log "Restore complete."
}

usage() {
    cat <<EOF
Usage: $0 [COMMAND]

Commands:
  update             Back up, pull source and images, deploy, and verify (default)
  deploy             Sync Compose, start services, and verify
  backup             Create a PostgreSQL backup archive
  restore FILE       Replace the database from a backup archive
  pull               Pull configured container images without restarting
  status             Show service status
  logs [SERVICE]     Follow logs for all services or one service
  stop               Stop services without deleting data
  help               Show this help
EOF
}

main() {
    local command="${1:-update}"

    mkdir -p "$OPENWISH_DEPLOY_DIR"
    require_command docker
    docker compose version >/dev/null 2>&1 || fail "Docker Compose v2 is required."

    case "$command" in
        update)
            require_command curl
            update
            ;;
        deploy)
            require_command curl
            sync_compose
            compose up -d --remove-orphans
            wait_for_health || fail "OpenWish did not become healthy."
            ;;
        backup)
            backup
            ;;
        restore)
            require_command curl
            restore "${2:-}"
            ;;
        pull)
            sync_compose
            compose pull
            ;;
        status)
            compose ps
            ;;
        logs)
            if [ -n "${2:-}" ]; then
                compose logs --follow --tail 200 "$2"
            else
                compose logs --follow --tail 200
            fi
            ;;
        stop)
            compose stop
            ;;
        help|-h|--help)
            usage
            ;;
        *)
            usage >&2
            fail "Unknown command: $command"
            ;;
    esac
}

main "$@"
