#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

run_id="$(date -u +%Y%m%d%H%M%S)-$$"
project_name="${OPENWISH_VERIFICATION_PROJECT:-openwish-verification-${run_id}}"
verification_image="${OPENWISH_VERIFICATION_IMAGE:-openwish-verification-app:${run_id}}"
playwright_image="openwish-playwright:1.65.1-${run_id}"
invite_test_image="openwish-invite-tests:${run_id}"
release_version="${OPENWISH_RELEASE_VERSION:-$(tr -d '[:space:]' < version.txt)}"
export OPENWISH_VERIFICATION_IMAGE="$verification_image"
built_verification_image=false
built_invite_test_image=false
built_playwright_image=false
compose=(docker compose -p "$project_name" -f compose.verify.yml)
evidence_directory="${OPENWISH_EVIDENCE_DIR:-$repository_root/pr-assets/verification}"
walkthrough_directory="${OPENWISH_WALKTHROUGH_DIR:-$repository_root/pr-assets/walkthrough}"
docker_repository_root="$repository_root"
browser_shard="${OPENWISH_BROWSER_SHARD:-}"
if [[ -n "$browser_shard" && "$browser_shard" != "core" && "$browser_shard" != "independent" ]]; then
  echo "Unknown browser shard: $browser_shard" >&2
  exit 1
fi

# Docker runs on Mate's host, while this script runs in Mate's container. Keep
# file checks on the mounted workspace path but give Docker host-visible sources.
if [[ -n "${MATE_HOST_WORKSPACE:-}" && "$repository_root" == /workspace/* ]]; then
  docker_repository_root="${MATE_HOST_WORKSPACE%/}/${repository_root#/workspace/}"
fi

docker_evidence_directory="${evidence_directory/#$repository_root/$docker_repository_root}"
docker_walkthrough_directory="${walkthrough_directory/#$repository_root/$docker_repository_root}"

cleanup() {
  local exit_code=$?
  "${compose[@]}" logs --no-color web > "$evidence_directory/openwish-e2e-web.log" 2>&1 || true
  if ((exit_code != 0)); then
    cat "$evidence_directory/openwish-e2e-web.log" >&2
  fi
  "${compose[@]}" down --remove-orphans
  if [[ "$built_verification_image" == "true" ]]; then
    docker image rm "$verification_image" >/dev/null 2>&1 || true
  fi
  if [[ "$built_invite_test_image" == "true" ]]; then
    docker image rm "$invite_test_image" >/dev/null 2>&1 || true
  fi
  if [[ "$built_playwright_image" == "true" ]]; then
    docker image rm "$playwright_image" >/dev/null 2>&1 || true
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
  --tag "$playwright_image" \
  .agents/skills/screenshot
built_playwright_image=true

if [[ -z "${OPENWISH_PREBUILT_VERIFICATION_IMAGE:-}" ]]; then
  docker build \
    --tag "$verification_image" \
    --build-arg BUILD_VERSION="$(tr -d '[:space:]' < version.txt)" \
    --build-arg GIT_SHA="$(git rev-parse --short HEAD)" \
    --file src/OpenWish.Web/Dockerfile \
    src
  built_verification_image=true
fi

if [[ "$browser_shard" != "independent" ]]; then
  docker build \
    --tag "$invite_test_image" \
    --file .agents/skills/screenshot/Dockerfile.tests \
    src
  built_invite_test_image=true
fi

"${compose[@]}" up --detach --wait

web_container="$("${compose[@]}" ps -q web)"
network_name="$(docker inspect "$web_container" --format '{{range $name, $_ := .NetworkSettings.Networks}}{{$name}}{{end}}')"

docker run --rm \
  --ipc=host \
  --network "$network_name" \
  --env OPENWISH_BASE_URL=http://web:8080 \
  --env OPENWISH_MAIL_URL=http://mail:8025 \
  --env "OPENWISH_RELEASE_VERSION=$release_version" \
  --env "OPENWISH_BROWSER_SHARD=$browser_shard" \
  --env OPENWISH_EVIDENCE_DIR=/evidence \
  --env OPENWISH_WALKTHROUGH_DIR=/walkthrough \
  --volume "$docker_evidence_directory:/evidence" \
  --volume "$docker_walkthrough_directory:/walkthrough" \
  "$playwright_image"

if [[ "$browser_shard" != "independent" ]]; then
  docker run --rm \
    --network "$network_name" \
    --env "OPENWISH_TEST_POSTGRES=Host=db;Port=5432;Database=OpenWish;Username=openwish;Password=openwish-verification" \
    "$invite_test_image" | tee "$evidence_directory/openwish-invite-concurrency-test.log"
fi

if [[ ! -s "$evidence_directory/openwish-e2e-result.json" ]]; then
  docker run --rm --entrypoint tar \
    --volume "$docker_evidence_directory:/evidence:ro" \
    "$playwright_image" -C /evidence -cf - . |
    tar -C "$evidence_directory" -xf -
  docker run --rm --entrypoint tar \
    --volume "$docker_walkthrough_directory:/walkthrough:ro" \
    "$playwright_image" -C /walkthrough -cf - . |
    tar -C "$walkthrough_directory" -xf -
fi

if [[ "$browser_shard" != "independent" ]]; then
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
fi

if [[ "$browser_shard" != "independent" ]]; then
test -s "$walkthrough_directory/home-dashboard.png"
test -s "$walkthrough_directory/readme-home.png"
test -s "$walkthrough_directory/home-mobile.png"
test -s "$walkthrough_directory/navigation-mobile.png"
test -s "$walkthrough_directory/wishlists.png"
test -s "$walkthrough_directory/wishlist-details.png"
test -s "$walkthrough_directory/wishlist-budget-filter.png"
test -s "$walkthrough_directory/wishlist-ranking.png"
test -s "$walkthrough_directory/wishlist-ranking-mobile.png"
test -s "$walkthrough_directory/readme-wishlist.png"
test -s "$walkthrough_directory/wishlist-item-added.png"
test -s "$walkthrough_directory/added-wishlist-item.png"
test -s "$walkthrough_directory/add-wishlist-item.png"
test -s "$walkthrough_directory/events.png"
test -s "$walkthrough_directory/create-event.png"
test -s "$walkthrough_directory/event-details.png"
test -s "$walkthrough_directory/event-wishlists.png"
test -s "$walkthrough_directory/event-wishlists-mobile.png"
test -s "$walkthrough_directory/readme-exchange.png"
test -s "$walkthrough_directory/event-management.png"
test -s "$walkthrough_directory/event-participant-removal.png"
test -s "$walkthrough_directory/invitation-decline-dialog.png"
test -s "$walkthrough_directory/invitation-accepted.png"
test -s "$walkthrough_directory/invitation-accepted-mobile.png"
test -s "$walkthrough_directory/event-invitation-filters-desktop.png"
test -s "$walkthrough_directory/event-invitation-filters-mobile.png"
test -s "$walkthrough_directory/invitation-composer.png"
test -s "$walkthrough_directory/event-wishlist-reminder-after-draw.png"
test -s "$walkthrough_directory/event-wishlist-reminder-after-draw-mobile.png"
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
fi
if [[ "$browser_shard" != "core" ]]; then
  for screenshot in invitation-email-desktop invitation-email-mobile invitation-join-choice register-without-account \
    connect-app-consent connect-app-consent-mobile connected-app-ideas connected-app-ideas-mobile \
    connected-app-edit-note connected-apps connected-apps-mobile connected-apps-mobile-dark \
    connected-apps-editor connected-apps-editor-mobile connected-apps-editor-dark \
    connected-apps-search-empty connected-apps-empty connected-app-disconnect \
    first-run-checklist friend-suggestions exchange-revealed exchange-revealed-mobile \
    stale-ideas archived-ideas grid-inline-coordination dashboard-gifts-to-buy dashboard-birthdays \
    account-birthday dashboard-birthdays-mobile dashboard-birthday-gift-nudge \
    wishlist-gift-hints wishlist-gift-hints-mobile account-gift-hints gift-plan gift-plan-bought \
    gift-plan-mobile gift-history gift-history-mobile duplicate-gift-warning duplicate-gift-warning-mobile \
    received-ideas-form received-ideas received-ideas-mobile run-again run-again-result run-again-mobile \
    connect-in-person connect-in-person-mobile friend-join-confirm friend-join-confirm-mobile; do
    test -s "$walkthrough_directory/$screenshot.png"
  done
fi
jq -e '.passed == true' "$evidence_directory/openwish-e2e-result.json" >/dev/null
if [[ "$browser_shard" != "independent" ]]; then
  jq -e '[.scenarios[].scenario] | index("owner-desktop") != null and index("guest-collaboration") != null' \
    "$evidence_directory/openwish-e2e-result.json" >/dev/null
fi
if [[ "$browser_shard" != "core" ]]; then
  jq -e '[.scenarios[].scenario] | index("responsive-sweep") != null and index("feature-day-outcomes") != null and index("occasions-outcomes") != null and index("occasion-reminders") != null and index("family-exchange") != null and index("registration-and-first-match") != null and index("account-free-guests") != null and index("connected-app") != null' \
    "$evidence_directory/openwish-e2e-result.json" >/dev/null
fi

web_log="$evidence_directory/openwish-e2e-web.log"
"${compose[@]}" logs web > "$web_log"
if grep -Eiq 'Unhandled exception|Request finished HTTP/[0-9.]+ 5[0-9]{2}|Database migration failed|DbUpdateConcurrencyException|concurrency conflict|warn: Microsoft.EntityFrameworkCore.*[Cc]oncurren' "$web_log"; then
  echo "Server logs contain a failed request, exception, or concurrency conflict." >&2
  exit 1
fi
