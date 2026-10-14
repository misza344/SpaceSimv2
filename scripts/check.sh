#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$SPACESIM_REPO"
dotnet restore SpaceSim.sln --locked-mode
dotnet build SpaceSim.sln --no-restore
dotnet test SpaceSim.sln --no-build --no-restore
log="$(mktemp)"
trap 'rm -f -- "$log"' EXIT
"$GODOT" --headless --path src/SpaceSim.Game --editor --import 2>&1 | tee "$log"
if rg -q '^ERROR:' "$log"; then exit 1; fi
timeout 90 "$GODOT" --headless --path src/SpaceSim.Game -- --smoke-test 2>&1 | tee "$log"
if rg -q '^ERROR:|SMOKE FAIL' "$log"; then exit 1; fi
rg -q 'SMOKE PASS 3D:' "$log"
