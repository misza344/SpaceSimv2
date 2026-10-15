#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$SPACESIM_REPO"

# Download once, verify against the official release SHA512-SUMS.txt.
archive="$SPACESIM_TOOLS/downloads/Godot_v4.6.3-stable_mono_export_templates.tpz"
mkdir -p "$(dirname "$archive")" dist
if [[ ! -f "$archive" ]]; then
  curl --fail --location --retry 2 'https://github.com/godotengine/godot/releases/download/4.6.3-stable/Godot_v4.6.3-stable_mono_export_templates.tpz' -o "$archive.part"
  mv "$archive.part" "$archive"
fi
printf '%s  %s\n' '6756c4c45cc711652fab0f5fbb75459f372f1f4b6cca6bb48e8953c25369418845c8cc39fcda0756effd69d0ecaf50e5653ebb9233b8e33f3bde5d8a81635168' "$archive" | sha512sum --check --status
python3 - "$archive" "$XDG_DATA_HOME/godot/export_templates/4.6.3.stable.mono" <<'PY'
from pathlib import Path
import sys, zipfile
root = Path(sys.argv[2]); root.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(sys.argv[1]) as z:
    for n in z.namelist():
        name = Path(n).name
        if name == 'version.txt' or ('windows_' in name and 'x86_64' in name) or name in ('linux_debug.x86_64', 'linux_release.x86_64'):
            target = root / name
            target.write_bytes(z.read(n)); target.chmod(0o755)
PY

dotnet restore SpaceSim.sln --locked-mode
dotnet restore src/SpaceSim.Game/SpaceSim.Game.csproj -p:Configuration=ExportRelease --locked-mode
stage="$(mktemp -d "$SPACESIM_REPO/dist/.portable-build.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
package="$stage/SpaceSim-0.5-3D-win-x64-portable"
mkdir -p "$package/licenses"
"$GODOT" --headless --path src/SpaceSim.Game --export-release 'Windows Portable' "$package/SpaceSim.exe" 2>&1 | tee "$stage/export.log"
# Some Godot export-plugin failures return exit code zero; reject those explicitly.
if rg -q '^ERROR:|completed with warnings' "$stage/export.log"; then
  echo 'Export reported errors or warnings; refusing to package.' >&2
  exit 1
fi
cp docs/portable-windows.txt "$package/CZYTAJ-MNIE.txt"
cp docs/licenses/Godot-*.txt "$package/licenses/"
python3 scripts/package-windows.py "$package" "$SPACESIM_REPO/dist" "$NUGET_PACKAGES"
