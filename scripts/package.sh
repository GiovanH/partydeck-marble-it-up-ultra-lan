#!/usr/bin/env bash
# Build and zip a release

set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
"$root/scripts/build.sh" "$@"

version="$(sed -n 's#.*<Version>\(.*\)</Version>.*#\1#p' "$root/Mod/MIUULan.csproj")"
name="MIUULan-$version"
stage="$root/dist/$name"

rm -rfv "$stage" "$root/dist/$name.zip"
mkdir -p "$stage/Mods/MIUULan"

cp "$root/build/MIUULan.dll" "$stage/Mods/MIUULan/"
cp "$root/scripts/install.sh" "$stage/"
(cd "$root/dist" && zip -qr "$name.zip" "$name")

rm -rf "$stage"

echo "Packaged $root/dist/$name.zip"
