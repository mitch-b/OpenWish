#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

run_id="$(date -u +%Y%m%d%H%M%S)-$$"
project_name="${OPENWISH_VERIFICATION_PROJECT:-openwish-verification-${run_id}}"
verification_image="${OPENWISH_VERIFICATION_IMAGE:-openwish-verification-app:${run_id}}"
export OPENWISH_VERIFICATION_IMAGE="$verification_image"
built_verification_image=false
compose=(docker compose -p "$project_name" -f compose.verify.yml)
evidence_directory="$repository_root/.docs/images/verification"
walkthrough_directory="$repository_root/.docs/images/walkthrough"
docker_repository_root="$repository_root"

# Docker runs on Mate's host, while this script runs in Mate's container. Keep
# file checks on the mounted workspace path but give Docker host-visible sources.
if [[ -n "${MATE_HOST_WORKSPACE:-}" && "$repository_root" == /workspace/* ]]; then
  docker_repository_root="${MATE_HOST_WORKSPACE%/}/${repository_root#/workspace/}"
fi

docker_evidence_directory="$docker_repository_root/.docs/images/verification"
docker_walkthrough_directory="$docker_repository_root/.docs/images/walkthrough"

cleanup() {
  "${compose[@]}" down --remove-orphans
  if [[ "$built_verification_image" == "true" ]]; then
    docker image rm "$verification_image" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

mkdir -p "$evidence_directory"
mkdir -p "$walkthrough_directory"
rm -f \
  "$evidence_directory"/*.png \
  "$evidence_directory/openwish-e2e-result.json"

docker build \
  --tag openwish-playwright:1.63.0 \
  .agents/skills/screenshot

if [[ -z "${OPENWISH_PREBUILT_VERIFICATION_IMAGE:-}" ]]; then
  docker build \
    --tag "$verification_image" \
    --build-arg BUILD_VERSION="$(tr -d '[:space:]' < version.txt)" \
    --build-arg GIT_SHA="$(git rev-parse --short HEAD)" \
    --file src/OpenWish.Web/Dockerfile \
    src
  built_verification_image=true
fi

"${compose[@]}" up --detach --wait

web_container="$("${compose[@]}" ps -q web)"
network_name="$(docker inspect "$web_container" --format '{{range $name, $_ := .NetworkSettings.Networks}}{{$name}}{{end}}')"

docker run --rm \
  --ipc=host \
  --network "$network_name" \
  --env OPENWISH_BASE_URL=http://web:8080 \
  --env OPENWISH_EVIDENCE_DIR=/evidence \
  --env OPENWISH_WALKTHROUGH_DIR=/walkthrough \
  --volume "$docker_evidence_directory:/evidence" \
  --volume "$docker_walkthrough_directory:/walkthrough" \
  openwish-playwright:1.63.0

test -s "$walkthrough_directory/home-dashboard.png"
test -s "$walkthrough_directory/home-mobile.png"
test -s "$walkthrough_directory/wishlists.png"
test -s "$walkthrough_directory/wishlist-details.png"
test -s "$walkthrough_directory/events.png"
test -s "$walkthrough_directory/event-details.png"
test -s "$walkthrough_directory/friends.png"
test -s "$walkthrough_directory/notifications.png"
jq -e '.passed == true' "$evidence_directory/openwish-e2e-result.json" >/dev/null

if "${compose[@]}" logs web | grep -Eiq 'Unhandled exception|Request finished HTTP/[0-9.]+ 5[0-9]{2}|Database migration failed'; then
  echo "Server logs contain a failed request or unhandled exception." >&2
  exit 1
fi
