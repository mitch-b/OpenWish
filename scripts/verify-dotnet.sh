#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
results_directory="${OPENWISH_TEST_RESULTS_DIR:-$repository_root/src/TestResults}"
mkdir -p "$results_directory"
results_directory="$(cd "$results_directory" && pwd)"
cd "$repository_root/src"

echo "Restoring the complete solution..."
dotnet restore

echo "Checking formatting and naming rules..."
dotnet format --verify-no-changes --no-restore

echo "Building the complete solution..."
dotnet build --no-restore

echo "Running all tests, including PostgreSQL-backed web tests..."
dotnet test --no-build --logger trx --results-directory "$results_directory"
