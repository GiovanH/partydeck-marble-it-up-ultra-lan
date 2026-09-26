#!/usr/bin/env bash
# Install MIUULan into a stock Marble It Up! Ultra (Linux, Steam) installation:
# the MIUU Mod Loader, the MIUULan mod, and steam_appid.txt so instances can be
# started without Steam's launcher.
#
# Usage: install.sh [--console] [GameDir]
#   --console  also install MIUU Console Unlocker (the ~ console, for lanHost/lanJoin)
#   GameDir    defaults to the usual Steam library path
set -euo pipefail

LOADER_URL="https://codeberg.org/thearst3rd/miuu-mod-loader/releases/download/v0.1.1/miuu-mod-loader-linux-v0.1.1.zip"
LOADER_SHA256="72aa5de79602e4eeefa0780fbd03dc608b538a5636a28655ab71d5203fb117da"

CONSOLE_URL="https://codeberg.org/thearst3rd/miuu-console-unlocker/releases/download/v0.2.0/miuu-console-unlocker-v0.2.0.zip"
CONSOLE_SHA256="a63a21f462664fb4036cd876a9c6d28e38e7cf0b9cfe111b3ade12eb0769b652"

STEAM_APPID=864060

console=0
game="$HOME/.local/share/Steam/steamapps/common/Marble It Up!"
for arg in "$@"; do
    case "$arg" in
        --console) console=1 ;;
        *) game="$arg" ;;
    esac
done

die() { echo "error: $*" >&2; exit 1; }

for tool in curl unzip sha256sum; do
    command -v "$tool" >/dev/null || die "Required tool $tool missing from path"
done

[ -x "$game/MarbleItUp.x86_64" ] && [ -d "$game/MarbleItUp_Data" ] \
    || die "no Linux Marble It Up! Ultra install at: $game (pass the game directory as an argument)"

# The DLL sits next to this script in a release zip, or in build/ in a source checkout.
here="$(cd "$(dirname "$0")" && pwd)"
dll=""
for candidate in "$here/Mods/MIUULan/MIUULan.dll" "$here/../build/MIUULan.dll"; do
    if [ -f "$candidate" ]; then dll="$candidate"; break; fi
done
[ -n "$dll" ] || die "MIUULan.dll not found; run scripts/build.sh first"

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

fetch() { # url sha256 dest
    url="$1"
    hash="$2"
    outpath="$3"
    # reuse existing
    [ -f "$outpath" ] && echo "$hash  $outpath" | sha256sum -c --quiet - && return

    curl -fsSL "$url" -o "$outpath" || die "download failed: $url"
    echo "$hash  $outpath" | sha256sum -c --quiet - || die "checksum mismatch: $url"
}

echo "Installing MIUU Mod Loader..."
fetch "$LOADER_URL" "$LOADER_SHA256" "$tmp/loader.zip"
unzip -oq "$tmp/loader.zip" -d "$game"
chmod +x "$game/run.sh"

if [ "$console" = 1 ]; then
    echo "Installing MIUU Console Unlocker..."
    fetch "$CONSOLE_URL" "$CONSOLE_SHA256" "$tmp/console.zip"
    # don't overwrite config.json
    unzip -nq "$tmp/console.zip" -d "$game/Mods"
    unzip -oq "$tmp/console.zip" ConsoleUnlocker/ConsoleUnlocker.dll -d "$game/Mods"
fi

echo "Installing MIUULan..."
mkdir -p "$game/Mods/MIUULan"
cp "$dll" "$game/Mods/MIUULan/MIUULan.dll"

cat <<EOF

Done: $game

Single instance through Steam: set the game's launch options to
    ./run.sh %command%

LAN splitscreen, one instance per player (N = 0 hosts, 1+ join), each with its own profile:
    cd "$game"
    XDG_CONFIG_HOME=~/.local/share/miuulan/p1 ./run.sh ./MarbleItUp.x86_64 -player 0 -unlockcosmetics -randomskin -nosteam
    XDG_CONFIG_HOME=~/.local/share/miuulan/p2 ./run.sh ./MarbleItUp.x86_64 -player 1 -unlockcosmetics -randomskin -nosteam
EOF
