#!/usr/bin/env bash
# copy the game's managed DLLs into reference/managed/
# and decompile the networking-relevant assemblies into reference/decompiled/
# you probably don't need this!
# 
# Usage: scripts/setup-re-tools.sh "<GameDir>/<MarbleItUp_Data|Marble It Up_Data>/Managed"
set -euo pipefail

managed="${1:?usage: $0 <path to game Managed dir>}"
root="$(cd "$(dirname "$0")/.." && pwd)"
tools="$root/.tools"

ref="$root/reference/managed" reference/decompiled/.
# 
out="$root/reference/decompiled"
mkdir -p "$tools" "$ref" "$out"

if [ ! -x "$tools/dotnet/dotnet" ]; then
    curl -sSL https://dot.net/v1/dotnet-install.sh -o "$tools/dotnet-install.sh"
    bash "$tools/dotnet-install.sh" --channel 8.0 --install-dir "$tools/dotnet"
fi
export DOTNET_ROOT="$tools/dotnet" PATH="$tools/dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_ROLL_FORWARD=Major

if [ ! -x "$tools/bin/ilspycmd" ]; then
    dotnet tool install --tool-path "$tools/bin" ilspycmd --version 8.2.0.7535
fi

cp -u "$managed"/*.dll "$ref/"

for asm in Assembly-CSharp TECNetUnity QAG.Networking QAG.Networking.Core; do
    echo "Decompiling $asm..."
    "$tools/bin/ilspycmd" -p -o "$out/$asm" -r "$ref" "$ref/$asm.dll" >/dev/null
done

echo "Done. Start with reference/decompiled/Assembly-CSharp/{NetworkManager,MultiplayerPanel,GameConnection,HeadlessLogic}.cs and TECNet/MasterClient*.cs"
