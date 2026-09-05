#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 || ! "$1" =~ ^(major|minor|patch)$ ]]; then
  echo "Usage: scripts/bump-version.sh major|minor|patch" >&2
  exit 2
fi

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
version_file="$repository_root/version.txt"
props_file="$repository_root/src/Directory.Build.props"
current_version="$(tr -d '[:space:]' < "$version_file")"

if [[ ! "$current_version" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
  echo "version.txt must contain a semantic version." >&2
  exit 1
fi

major="${BASH_REMATCH[1]}"
minor="${BASH_REMATCH[2]}"
patch="${BASH_REMATCH[3]}"

case "$1" in
  major)
    major=$((major + 1))
    minor=0
    patch=0
    ;;
  minor)
    minor=$((minor + 1))
    patch=0
    ;;
  patch)
    patch=$((patch + 1))
    ;;
esac

next_version="$major.$minor.$patch"
printf '%s\n' "$next_version" > "$version_file"
sed -i -E "s#<Version>[^<]+</Version>#<Version>$next_version</Version>#" "$props_file"

grep -qx "$next_version" "$version_file"
grep -q "<Version>$next_version</Version>" "$props_file"
printf 'Bumped OpenWish from %s to %s.\n' "$current_version" "$next_version"
