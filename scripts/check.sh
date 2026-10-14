#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$SPACESIM_REPO"
dotnet restore SpaceSim.sln --locked-mode
dotnet build SpaceSim.sln --no-restore
dotnet test SpaceSim.sln --no-build --no-restore
"$GODOT" --headless --path src/SpaceSim.Game --editor --import
"$GODOT" --headless --path src/SpaceSim.Game -- --smoke-test
