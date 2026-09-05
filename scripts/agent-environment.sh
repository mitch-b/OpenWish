#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

action="${1:-deploy}"
project_name="${OPENWISH_AGENT_PROJECT:-openwish-agent}"
environment_file="$repository_root/.env.agent"
lock_file="${TMPDIR:-/tmp}/openwish-agent-deploy.lock"
compose=(docker compose -p "$project_name")

if [[ -f "$environment_file" ]]; then
  compose+=(--env-file "$environment_file")
  set -a
  # shellcheck disable=SC1090
  source "$environment_file"
  set +a
fi

compose+=(-f compose.agent.yml)
agent_port="${OPENWISH_AGENT_PORT:-9090}"

wait_until_healthy() {
  for _ in $(seq 1 60); do
    if curl --fail --silent "http://localhost:${agent_port}/alive" >/dev/null 2>&1; then
      return 0
    fi
    sleep 2
  done

  return 1
}

seed_agent_data() {
  local cookie_file
  cookie_file="$(mktemp "${TMPDIR:-/tmp}/openwish-agent-cookie.XXXXXX")"

  if ! curl --fail --silent \
    --cookie-jar "$cookie_file" \
    --request POST \
    "http://localhost:${agent_port}/auth/dev-login?persona=owner" >/dev/null ||
    ! curl --fail --silent \
      --cookie "$cookie_file" \
      --request POST \
      "http://localhost:${agent_port}/auth/dev-seed" >/dev/null; then
    rm -f "$cookie_file"
    return 1
  fi

  rm -f "$cookie_file"
}

deploy() {
  exec 9>"$lock_file"
  if ! flock --nonblock 9; then
    echo "Another agent environment deployment is already running." >&2
    exit 1
  fi

  local run_id candidate_image previous_container previous_image rollback_image
  run_id="$(date -u +%Y%m%d%H%M%S)-$$"
  candidate_image="openwish-agent:candidate-${run_id}"
  rollback_image="openwish-agent:rollback-${run_id}"
  previous_image=""

  echo "Building the candidate..."
  docker build \
    --tag "$candidate_image" \
    --build-arg BUILD_VERSION="$(tr -d '[:space:]' < version.txt)" \
    --build-arg GIT_SHA="$(git rev-parse --short HEAD)" \
    --file src/OpenWish.Web/Dockerfile \
    src

  echo "Verifying the candidate in an ephemeral environment..."
  if ! OPENWISH_PREBUILT_VERIFICATION_IMAGE=1 \
      OPENWISH_VERIFICATION_IMAGE="$candidate_image" \
      OPENWISH_VERIFICATION_PROJECT="openwish-verification-${run_id}" \
      scripts/verify-e2e.sh; then
    docker image rm "$candidate_image" >/dev/null 2>&1 || true
    return 1
  fi

  previous_container="$("${compose[@]}" ps -q web 2>/dev/null || true)"
  if [[ -n "$previous_container" ]]; then
    previous_image="$(docker inspect "$previous_container" --format '{{.Image}}')"
    docker tag "$previous_image" "$rollback_image"
  fi

  echo "Promoting the candidate to the isolated agent environment..."
  if ! OPENWISH_AGENT_IMAGE="$candidate_image" "${compose[@]}" up --detach --wait --force-recreate; then
    restore_previous "$previous_image" "$rollback_image"
    remove_transient_images "$candidate_image" "$rollback_image"
    return 1
  fi

  if ! wait_until_healthy; then
    echo "The candidate failed its post-promotion health check; restoring the previous image." >&2
    restore_previous "$previous_image" "$rollback_image"
    remove_transient_images "$candidate_image" "$rollback_image"
    return 1
  fi

  if ! seed_agent_data; then
    echo "The candidate failed to load its synthetic review data; restoring the previous image." >&2
    restore_previous "$previous_image" "$rollback_image"
    remove_transient_images "$candidate_image" "$rollback_image"
    return 1
  fi

  docker tag "$candidate_image" openwish-agent:local
  remove_transient_images "$candidate_image" "$rollback_image"
  echo "Agent environment is ready at http://localhost:${agent_port}"
}

remove_transient_images() {
  local candidate_image="$1"
  local rollback_image="$2"

  docker image rm "$candidate_image" >/dev/null 2>&1 || true
  docker image rm "$rollback_image" >/dev/null 2>&1 || true
}

restore_previous() {
  local previous_image="$1"
  local rollback_image="$2"

  if [[ -n "$previous_image" ]]; then
    OPENWISH_AGENT_IMAGE="$rollback_image" "${compose[@]}" up --detach --wait --force-recreate
    wait_until_healthy
  else
    "${compose[@]}" down --remove-orphans
  fi
}

case "$action" in
  deploy|start)
    deploy
    ;;
  status)
    "${compose[@]}" ps
    ;;
  logs)
    "${compose[@]}" logs --follow web
    ;;
  stop)
    "${compose[@]}" down --remove-orphans
    ;;
  reset)
    "${compose[@]}" down --volumes --remove-orphans
    ;;
  *)
    echo "Usage: $0 {deploy|start|status|logs|stop|reset}" >&2
    exit 2
    ;;
esac
