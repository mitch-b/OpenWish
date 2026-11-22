#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
root="$PWD"
assets="${1:-$root/pr-assets/render-benchmark}"
mkdir -p "$assets"
assets="$(cd "$assets" && pwd)"
rm -f "$assets/summary.md" "$assets/failed-web.log" \
  "$assets/Auto.json" "$assets/Server.json" "$assets/WebAssembly.json"
work="$(mktemp -d)"
project="openwish-render-benchmark-$(date -u +%Y%m%d%H%M%S)-$$"
export OPENWISH_VERIFICATION_IMAGE="$project:app"
browser_image="$project:browser"
compose=(docker compose -p "$project" -f "$root/compose.verify.yml" -f "$work/mode.yml")
cleanup() {
  local result=$?
  if ((result != 0)); then
    "${compose[@]}" logs --no-color web > "$assets/failed-web.log" 2>&1 || true
  fi
  "${compose[@]}" down --volumes --remove-orphans >&2 || true
  docker image rm "$OPENWISH_VERIFICATION_IMAGE" "$browser_image" >/dev/null 2>&1 || true
  if [[ "$work" == "${TMPDIR:-/tmp}"/tmp.* ]]; then
    find "$work" -depth -delete
  fi
}
trap cleanup EXIT

# Export tracked sources only, including worktree edits.
git ls-files -z src | tar --null -T - -cf - | tar -xf - -C "$work"
skill="$root/.agents/skills/openwish-render-modes"
python3 "$skill/instrument.py" "$work/src" > "$assets/components.json"

docker build -q -t "$OPENWISH_VERIFICATION_IMAGE" \
  --build-arg BUILD_VERSION="$(tr -d '[:space:]' < version.txt)" \
  --build-arg GIT_SHA="$(git rev-parse --short HEAD)" \
  -f "$work/src/OpenWish.Web/Dockerfile" "$work/src"
docker build -q -t "$browser_image" .agents/skills/screenshot

docker_root="$root"
if [[ -n "${MATE_HOST_WORKSPACE:-}" && "$root" == /workspace/* ]]; then
  docker_root="${MATE_HOST_WORKSPACE%/}/${root#/workspace/}"
fi
docker_assets="${assets/#$root/$docker_root}"

for mode in Auto Server WebAssembly; do
  printf 'services:\n  web:\n    environment:\n      OPENWISH_BENCHMARK_MODE: %s\n' "$mode" > "$work/mode.yml"
  "${compose[@]}" up --detach --wait
  container="$("${compose[@]}" ps -q web)"
  echo "Measuring $mode"
  docker run --rm --ipc=host --network "container:$container" --user "$(id -u):$(id -g)" \
    --env "BENCHMARK_MODE=$mode" --env BENCHMARK_SAMPLES --env BENCHMARK_PROFILES \
    --volume "$docker_assets:/results" \
    --volume "$docker_root/.agents/skills/openwish-render-modes/benchmark.mjs:/opt/openwish-verifier/benchmark.mjs:ro" \
    --entrypoint node "$browser_image" benchmark.mjs
done

python3 "$skill/summarize.py" "$assets"
