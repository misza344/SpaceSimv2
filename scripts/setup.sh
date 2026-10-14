#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/env.sh"
[[ "$(uname -s)" == Linux && "$(uname -m)" == x86_64 ]] || { echo 'Automatyczny instalator wymaga Linux x86_64. Patrz README dla Windows.'; exit 1; }
mkdir -p "$SPACESIM_TOOLS/downloads" "$DOTNET_ROOT" "$SPACESIM_TOOLS/godot"
fetch_verified() {
  local url="$1" file="$2" checksum="$3"
  if [[ ! -f "$file" ]]; then
    curl --fail --location --retry 2 "$url" -o "$file.part"
    mv "$file.part" "$file"
  fi
  printf '%s  %s\n' "$checksum" "$file" | sha512sum --check --status
}
if [[ ! -x "$DOTNET_ROOT/dotnet" ]]; then
  fetch_verified 'https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.426/dotnet-sdk-8.0.426-linux-x64.tar.gz' "$SPACESIM_TOOLS/downloads/dotnet.tar.gz" '8141d01bdc53f604aed3bb661b604707a64209f8a947cd81bf95b4f518951593d77864069191f54ea5e84362a409da6b0f69074bf893b283d7e0cf93e2bf20a8'
  tar -xzf "$SPACESIM_TOOLS/downloads/dotnet.tar.gz" -C "$DOTNET_ROOT"
fi
if [[ ! -x "$GODOT" ]]; then
  fetch_verified 'https://github.com/godotengine/godot/releases/download/4.6.3-stable/Godot_v4.6.3-stable_mono_linux_x86_64.zip' "$SPACESIM_TOOLS/downloads/godot.zip" '962c4f5fd3a704de36c9dedcb2f5f6c973065ac8ed8fd6e12c7ebf7e48c3852d774bcef2ccf76b50b206c0c564ceea54315e2943e76e397529923e7de680508e'
  unzip -q -o "$SPACESIM_TOOLS/downloads/godot.zip" -d "$SPACESIM_TOOLS/godot"
  chmod +x "$GODOT"
fi
cd "$SPACESIM_REPO"
dotnet restore SpaceSim.sln --locked-mode
dotnet build SpaceSim.sln --no-restore
