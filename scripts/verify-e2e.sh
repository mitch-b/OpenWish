#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

run_id="$(date -u +%Y%m%d%H%M%S)-$$"
project_name="${OPENWISH_VERIFICATION_PROJECT:-openwish-verification-${run_id}}"
verification_image="${OPENWISH_VERIFICATION_IMAGE:-openwish-verification-app:${run_id}}"
release_version="${OPENWISH_RELEASE_VERSION:-$(tr -d '[:space:]' < version.txt)}"
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
  local exit_code=$?
  if ((exit_code != 0)); then
    "${compose[@]}" logs web >&2 || true
  fi
  "${compose[@]}" down --remove-orphans
  if [[ "$built_verification_image" == "true" ]]; then
    docker image rm "$verification_image" >/dev/null 2>&1 || true
  fi
  return "$exit_code"
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
  --env "OPENWISH_RELEASE_VERSION=$release_version" \
  --env OPENWISH_EVIDENCE_DIR=/evidence \
  --env OPENWISH_WALKTHROUGH_DIR=/walkthrough \
  --volume "$docker_evidence_directory:/evidence" \
  --volume "$docker_walkthrough_directory:/walkthrough" \
  openwish-playwright:1.63.0

dependent_cleanup="$("${compose[@]}" exec -T db \
  psql -U openwish -d OpenWish -Atc \
  "SELECT CASE
      WHEN EXISTS (
        SELECT 1
        FROM \"WishlistItems\" item
        WHERE item.\"PublicId\" = 'e2e-concurrent-delete-item'
          AND item.\"Deleted\"
      )
      AND EXISTS (
        SELECT 1
        FROM \"ItemComments\" comment
        JOIN \"WishlistItems\" item ON item.\"Id\" = comment.\"WishlistItemId\"
        WHERE item.\"PublicId\" = 'e2e-concurrent-delete-item'
          AND comment.\"Deleted\"
      )
      AND EXISTS (
        SELECT 1
        FROM \"ItemReservations\" reservation
        JOIN \"WishlistItems\" item ON item.\"Id\" = reservation.\"WishlistItemId\"
        WHERE item.\"PublicId\" = 'e2e-concurrent-delete-item'
          AND reservation.\"Deleted\"
      )
      AND NOT EXISTS (
        SELECT 1
        FROM \"ItemComments\" comment
        JOIN \"WishlistItems\" item ON item.\"Id\" = comment.\"WishlistItemId\"
        WHERE item.\"PublicId\" = 'e2e-concurrent-delete-item'
          AND NOT comment.\"Deleted\"
      )
      AND NOT EXISTS (
        SELECT 1
        FROM \"ItemReservations\" reservation
        JOIN \"WishlistItems\" item ON item.\"Id\" = reservation.\"WishlistItemId\"
        WHERE item.\"PublicId\" = 'e2e-concurrent-delete-item'
          AND NOT reservation.\"Deleted\"
      )
      AND NOT EXISTS (
        SELECT 1
        FROM \"ItemReactions\" reaction
        JOIN \"WishlistItems\" item ON item.\"Id\" = reaction.\"WishlistItemId\"
        WHERE item.\"PublicId\" = 'e2e-concurrent-delete-item'
          AND NOT reaction.\"Deleted\"
      )
      AND NOT EXISTS (
        SELECT 1
        FROM \"WillPurchases\" purchase
        JOIN \"WishlistItems\" item ON item.\"Id\" = purchase.\"WishlistItemId\"
        WHERE item.\"PublicId\" = 'e2e-concurrent-delete-item'
          AND NOT purchase.\"Deleted\"
      )
      THEN 'ok'
      ELSE 'failed'
    END;")"
if [[ "$dependent_cleanup" != "ok" ]]; then
  echo "Concurrent item deletion left active dependent records." >&2
  exit 1
fi

result_file="$evidence_directory/openwish-e2e-result.json"
jq '(.scenarios[] | select(.scenario == "owner-desktop").assertions) +=
    ["PostgreSQL dependent soft-delete cleanup"]' \
  "$result_file" > "$result_file.tmp"
mv "$result_file.tmp" "$result_file"

test -s "$walkthrough_directory/home-dashboard.png"
test -s "$walkthrough_directory/home-mobile.png"
test -s "$walkthrough_directory/navigation-mobile.png"
test -s "$walkthrough_directory/wishlists.png"
test -s "$walkthrough_directory/wishlist-details.png"
test -s "$walkthrough_directory/wishlist-item-added.png"
test -s "$walkthrough_directory/added-wishlist-item.png"
test -s "$walkthrough_directory/add-wishlist-item.png"
test -s "$walkthrough_directory/events.png"
test -s "$walkthrough_directory/create-event.png"
test -s "$walkthrough_directory/event-details.png"
test -s "$walkthrough_directory/event-management.png"
test -s "$walkthrough_directory/event-participant-removal.png"
test -s "$walkthrough_directory/invitation-decline-dialog.png"
test -s "$walkthrough_directory/friends.png"
test -s "$walkthrough_directory/notifications.png"
test -s "$walkthrough_directory/notification-delete-dialog.png"
test -s "$walkthrough_directory/reservation-cancel-confirmation.png"
test -s "$walkthrough_directory/reservation-cancel-confirmation-mobile.png"
test -s "$walkthrough_directory/comment-delete-confirmation.png"
test -s "$walkthrough_directory/comment-delete-confirmation-mobile.png"
test -s "$walkthrough_directory/event-delete-dialog.png"
test -s "$walkthrough_directory/account-settings.png"
test -s "$walkthrough_directory/account-settings-mobile.png"
test -s "$walkthrough_directory/account-deletion-mobile.png"
test -s "$walkthrough_directory/two-factor-settings.png"
test -s "$walkthrough_directory/two-factor-disable.png"
test -s "$walkthrough_directory/authenticator-setup-mobile.png"
test -s "$walkthrough_directory/account-recovery.png"
test -s "$walkthrough_directory/account-recovery-mobile.png"
test -s "$walkthrough_directory/whats-new.png"
jq -e '.passed == true' "$evidence_directory/openwish-e2e-result.json" >/dev/null

if "${compose[@]}" logs web | grep -Eiq 'Unhandled exception|Request finished HTTP/[0-9.]+ 5[0-9]{2}|Database migration failed|DbUpdateConcurrencyException|concurrency conflict'; then
  echo "Server logs contain a failed request, exception, or concurrency conflict." >&2
  exit 1
fi
