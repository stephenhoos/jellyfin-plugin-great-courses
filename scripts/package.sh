#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/Jellyfin.Plugin.GreatCourses/Jellyfin.Plugin.GreatCourses.csproj"
OUTPUT="$ROOT_DIR/artifacts/GreatCourses-plugin.zip"
TMP_DIR="$ROOT_DIR/package-tmp"
BUILD_DIR="$ROOT_DIR/Jellyfin.Plugin.GreatCourses/bin/Release/net9.0"

dotnet build "$PROJECT" -c Release

rm -rf "$TMP_DIR"
mkdir -p "$TMP_DIR" "$ROOT_DIR/artifacts"

cp "$BUILD_DIR/Jellyfin.Plugin.GreatCourses.dll" "$TMP_DIR/"
cp "$BUILD_DIR/Jellyfin.Plugin.GreatCourses.pdb" "$TMP_DIR/"
cp "$BUILD_DIR/Jellyfin.Plugin.GreatCourses.xml" "$TMP_DIR/"
cp "$BUILD_DIR/meta.json" "$TMP_DIR/"

rm -f "$OUTPUT"
(cd "$TMP_DIR" && zip -r "$OUTPUT" . -x '*/._*' '._*')
rm -rf "$TMP_DIR"

shasum -a 256 "$OUTPUT"
