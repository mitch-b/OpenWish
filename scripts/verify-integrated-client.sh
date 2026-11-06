#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"
client_source="${1:?Usage: scripts/verify-integrated-client.sh /absolute/path/to/the-atelier}"
[[ "$client_source" = /* && -f "$client_source/server/package-lock.json" ]] || {
  echo "Provide an absolute path to the actual companion checkout." >&2
  exit 1
}

run_id="$(date -u +%Y%m%d%H%M%S)-$$"
project_name="openwish-client-verification-$run_id"
export OPENWISH_VERIFICATION_IMAGE="openwish-client-web:$run_id"
export OPENWISH_ATELIER_VERIFICATION_IMAGE="openwish-client-atelier:$run_id"
export OPENWISH_CLIENT_CERTS
OPENWISH_CLIENT_CERTS="$(mktemp -d /tmp/openwish-client-certs.XXXXXX)"
compose=(docker compose -p "$project_name" -f compose.verify.yml -f compose.clients.verify.yml)

cleanup() {
  local result=$?
  if ((result != 0)); then
    "${compose[@]}" logs web atelier proxy >&2 || true
  fi
  "${compose[@]}" down --volumes --remove-orphans
  docker image rm "$OPENWISH_VERIFICATION_IMAGE" "$OPENWISH_ATELIER_VERIFICATION_IMAGE" >/dev/null 2>&1 || true
  rm -f "$OPENWISH_CLIENT_CERTS/key.pem" "$OPENWISH_CLIENT_CERTS/cert.pem"
  rmdir "$OPENWISH_CLIENT_CERTS"
  return "$result"
}
trap cleanup EXIT

echo "Building isolated OpenWish and actual Atelier client images..."
docker build --tag "$OPENWISH_ATELIER_VERIFICATION_IMAGE" \
  --file .agents/skills/screenshot/Dockerfile.atelier "$client_source"
docker run --rm "$OPENWISH_ATELIER_VERIFICATION_IMAGE" node --test
docker build --quiet --tag "$OPENWISH_VERIFICATION_IMAGE" \
  --build-arg "BUILD_VERSION=$(tr -d '[:space:]' < version.txt)" \
  --build-arg "GIT_SHA=$(git rev-parse --short HEAD)" \
  --file src/OpenWish.Web/Dockerfile src
docker build --quiet --tag openwish-playwright:1.64.1 .agents/skills/screenshot

openssl req -x509 -newkey rsa:2048 -nodes -days 1 \
  -keyout "$OPENWISH_CLIENT_CERTS/key.pem" -out "$OPENWISH_CLIENT_CERTS/cert.pem" \
  -subj /CN=wish.test -addext "subjectAltName=DNS:wish.test,DNS:atelier.test" >/dev/null 2>&1
echo "Starting isolated HTTPS reverse proxy and synthetic accounts..."
"${compose[@]}" up --detach --wait
web_container="$("${compose[@]}" ps -q web)"
network_name="$(docker inspect "$web_container" --format '{{range $name, $_ := .NetworkSettings.Networks}}{{$name}}{{end}}')"
mkdir -p .docs/images/verification
docker run --rm --ipc=host --network "$network_name" \
  --volume "$repository_root/.agents/skills/screenshot:/verification:ro" \
  --volume "$repository_root/.docs/images/verification:/evidence" \
  --entrypoint node openwish-playwright:1.64.1 /opt/openwish-verifier/integrated-client.mjs
