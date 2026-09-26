#!/usr/bin/env bash

# Write UserProperties.xml, build MIUULan.dll into build/, and copy to game directory.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
props_file="$root/Mod/UserProperties.xml"

game_dir="${1:-$HOME/.local/share/Steam/steamapps/common/Marble It Up!}"

if [ ! -f "$props_file" ]; then
    if [ ! -d "$game_dir/MarbleItUp_Data/Managed" ]; then
        echo "Game not found at: $game_dir" >&2
        echo "Usage: $0 <GameDir>" >&2
        exit 1
    fi
    sed "s#<GameDir></GameDir>#<GameDir>$game_dir</GameDir>#" "$root/Mod/UserProperties.xml.template" > "$props_file"
    echo "Wrote $props_file"
fi

# Prefer the project-local SDK from setup-re-tools.sh, else a system dotnet.
if [ -x "$root/.tools/dotnet/dotnet" ]; then
    export DOTNET_ROOT="$root/.tools/dotnet" PATH="$root/.tools/dotnet:$PATH"
fi
command -v dotnet >/dev/null || { echo "dotnet SDK not found; run scripts/setup-re-tools.sh or install .NET 8 SDK" >&2; exit 1; }
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# DEBUG_OVERLAY=1 builds in the corner status text (player, port, last LAN action).
dotnet build "$root/Mod" -c Release -nologo -v quiet ${DEBUG_OVERLAY:+-p:DebugOverlay=true}
dotnet build-server shutdown >/dev/null 2>&1 || true
echo "Built $root/build/MIUULan.dll"

cp -v "$root/build/MIUULan.dll" "$game_dir"
echo "OK!"
