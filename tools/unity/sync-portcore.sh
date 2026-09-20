#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT="$REPO_ROOT/src/PortCore/PortCore.csproj"
SOURCE="$REPO_ROOT/src/PortCore/bin/Release/netstandard2.1/TimeClickers.PortCore.dll"
DEST_DIR="$REPO_ROOT/unity/TimeCli/Assets/Plugins/TimeCli"
DEST="$DEST_DIR/TimeClickers.PortCore.dll"

echo "Building PortCore for Unity (netstandard2.1)..."
dotnet build "$PROJECT" -f netstandard2.1 -c Release

test -f "$SOURCE"
mkdir -p "$DEST_DIR"
cp -f "$SOURCE" "$DEST"

echo "Synced:"
echo "  $DEST"
