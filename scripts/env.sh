#!/usr/bin/env bash
SPACESIM_REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export SPACESIM_TOOLS="${SPACESIM_TOOLS:-$SPACESIM_REPO/.tools}"
export DOTNET_ROOT="$SPACESIM_TOOLS/dotnet"
export DOTNET_CLI_HOME="$SPACESIM_TOOLS/dotnet-home"
export NUGET_PACKAGES="$SPACESIM_TOOLS/nuget"
export XDG_DATA_HOME="$SPACESIM_TOOLS/data"
export XDG_CONFIG_HOME="$SPACESIM_TOOLS/config"
export XDG_CACHE_HOME="$SPACESIM_TOOLS/cache"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export PATH="$DOTNET_ROOT:$PATH"
export GODOT="$SPACESIM_TOOLS/godot/Godot_v4.6.3-stable_mono_linux_x86_64/Godot_v4.6.3-stable_mono_linux.x86_64"
mkdir -p "$DOTNET_CLI_HOME" "$XDG_CACHE_HOME"
