#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

project_name="${COMPOSE_PROJECT_NAME:-openwish-verification}"
compose=(docker compose -p "$project_name" -f compose.verify.yml)
evidence_directory="$repository_root/.docs/images/verification"

cleanup() {
  "${compose[@]}" down --remove-orphans
}
trap cleanup EXIT

mkdir -p "$evidence_directory"
rm -f \
  "$evidence_directory/openwish-home-desktop.png" \
  "$evidence_directory/openwish-home-mobile.png" \
  "$evidence_directory/openwish-e2e-result.json"

docker build \
  --tag openwish-playwright:1.63.0 \
  .agents/skills/screenshot

"${compose[@]}" up --build --detach --wait

web_container="$("${compose[@]}" ps -q web)"
network_name="$(docker inspect "$web_container" --format '{{range $name, $_ := .NetworkSettings.Networks}}{{$name}}{{end}}')"

docker run --rm \
  --ipc=host \
  --network "$network_name" \
  --env OPENWISH_BASE_URL=http://web:8080 \
  --env OPENWISH_EVIDENCE_DIR=/evidence \
  --volume "$evidence_directory:/evidence" \
  openwish-playwright:1.63.0

test -s "$evidence_directory/openwish-home-desktop.png"
test -s "$evidence_directory/openwish-home-mobile.png"
jq -e '.passed == true' "$evidence_directory/openwish-e2e-result.json" >/dev/null

if "${compose[@]}" logs web | grep -Eiq 'Unhandled exception|Request finished HTTP/[0-9.]+ 5[0-9]{2}|Database migration failed'; then
  echo "Server logs contain a failed request or unhandled exception." >&2
  exit 1
fi
