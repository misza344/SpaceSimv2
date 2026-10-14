#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$SPACESIM_REPO"
dotnet build src/SpaceSim.Game/SpaceSim.Game.csproj --no-restore
exec "$GODOT" --path src/SpaceSim.Game "$@"
