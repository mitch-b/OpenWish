#!/usr/bin/env bash

set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-${SCRIPT_DIR}/compose.yml}"
ENV_FILE="${ENV_FILE:-${SCRIPT_DIR}/.env}"

compose() {
    docker compose --file "$COMPOSE_FILE" --env-file "$ENV_FILE" "$@"
}

fail() {
    printf 'Error: %s\n' "$*" >&2
    exit 1
}

require_command() {
    command -v "$1" >/dev/null 2>&1 || fail "Required command not found: $1"
}

require_environment() {
    [[ -f "$ENV_FILE" ]] || fail "Missing $ENV_FILE. Copy .env.example to .env and configure it."
}

env_value() {
    local key="$1"
    local fallback="$2"
    local value

    value="$(sed -n "s/^${key}=//p" "$ENV_FILE" | tail -n 1)"
    printf '%s' "${value:-$fallback}"
}

backup_directory() {
    local configured
    configured="$(env_value BACKUP_DIR ./backups)"

    if [[ "$configured" = /* ]]; then
        printf '%s' "$configured"
    else
        printf '%s/%s' "$SCRIPT_DIR" "${configured#./}"
    fi
}

database_is_running() {
    [[ "$(compose ps --status running --quiet db)" != "" ]]
}

wait_for_database() {
    local attempts=60

    printf 'Waiting for PostgreSQL'
    while (( attempts > 0 )); do
        if compose exec -T db sh -c 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"' >/dev/null 2>&1; then
            printf ' ready\n'
            return
        fi
        printf '.'
        sleep 2
        ((attempts--))
    done
    printf '\n'
    fail "PostgreSQL did not become ready."
}

backup() {
    local directory timestamp temporary_file backup_file retention_days

    database_is_running || fail "The database service is not running."
    directory="$(backup_directory)"
    timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
    backup_file="${directory}/openwish-${timestamp}.dump"
    temporary_file="${backup_file}.tmp"
    retention_days="$(env_value BACKUP_RETENTION_DAYS 30)"
    [[ "$retention_days" =~ ^[0-9]+$ ]] || fail "BACKUP_RETENTION_DAYS must be a non-negative integer."

    mkdir -p "$directory"
    chmod 700 "$directory"
    trap 'rm -f -- "$temporary_file"' RETURN

    printf 'Backing up PostgreSQL to %s\n' "$backup_file"
    compose exec -T db sh -c \
        'pg_dump --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --format=custom --no-owner --no-privileges' \
        > "$temporary_file"
    [[ -s "$temporary_file" ]] || fail "PostgreSQL produced an empty backup."
    chmod 600 "$temporary_file"
    mv -- "$temporary_file" "$backup_file"
    trap - RETURN

    if (( retention_days > 0 )); then
        find "$directory" -maxdepth 1 -type f -name 'openwish-*.dump' \
            -mtime "+$((retention_days - 1))" -delete
    fi
    printf 'Backup complete.\n'
}

pull_images() {
    printf 'Pulling deployment images\n'
    compose pull
}

deploy() {
    printf 'Starting OpenWish\n'
    compose up --detach --remove-orphans
    wait_for_database
    compose ps
}

update() {
    if database_is_running; then
        backup
    else
        printf 'Database is not running; skipping the pre-update backup.\n'
    fi

    pull_images
    deploy
    printf 'Update complete.\n'
}

restore() {
    local backup_file="${1:-}"

    [[ -n "$backup_file" ]] || fail "Usage: $0 restore PATH_TO_DUMP"
    [[ -r "$backup_file" ]] || fail "Backup is not readable: $backup_file"
    database_is_running || fail "The database service is not running."

    printf 'This replaces the current OpenWish database with %s.\n' "$backup_file"
    read -r -p "Type RESTORE to continue: " confirmation
    [[ "$confirmation" == "RESTORE" ]] || fail "Restore cancelled."

    backup
    compose stop web
    trap 'compose up --detach web >/dev/null' RETURN
    compose exec -T db sh -c \
        'pg_restore --clean --if-exists --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --no-owner --no-privileges' \
        < "$backup_file"
    compose up --detach web
    trap - RETURN
    printf 'Restore complete.\n'
}

usage() {
    cat <<EOF
Usage: $0 COMMAND

Commands:
  deploy             Start or reconcile the deployment
  backup             Create a compressed PostgreSQL backup
  pull               Pull configured container images without restarting
  update             Back up, pull images, and reconcile the deployment
  restore FILE       Back up, then replace the database from FILE
  status             Show service status
  logs [SERVICE]     Follow logs for all services or one service
  stop               Stop services without deleting data
  help               Show this help
EOF
}

main() {
    local command="${1:-help}"
    require_command docker
    docker compose version >/dev/null 2>&1 || fail "Docker Compose v2 is required."

    if [[ "$command" != "help" && "$command" != "-h" && "$command" != "--help" ]]; then
        require_environment
    fi

    case "$command" in
        deploy)
            deploy
            ;;
        backup)
            backup
            ;;
        pull)
            pull_images
            ;;
        update)
            update
            ;;
        restore)
            restore "${2:-}"
            ;;
        status)
            compose ps
            ;;
        logs)
            if [[ -n "${2:-}" ]]; then
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
