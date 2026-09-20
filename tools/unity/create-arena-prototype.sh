#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT_PATH="$REPO_ROOT/unity/TimeCli"
LOG_DIR="$REPO_ROOT/out/unity"
LOG_PATH="$LOG_DIR/arena-prototype-batch.log"

UNITY_PATH="${UNITY_PATH:-}"
PRIVATE_VOXEL_JSON="${PRIVATE_VOXEL_JSON:-}"

if [[ -z "$UNITY_PATH" ]]; then
  if [[ "$(uname -s)" == "Darwin" ]]; then
    UNITY_PATH="/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity"
  else
    UNITY_PATH="$HOME/Unity/Hub/Editor/6000.3.24f1/Editor/Unity"
  fi
fi

if [[ ! -x "$UNITY_PATH" ]]; then
  echo "Unity executable not found or not executable:"
  echo "  $UNITY_PATH"
  echo "Set UNITY_PATH to the Unity 6000.3.24f1 executable."
  exit 1
fi

mkdir -p "$LOG_DIR"

echo "1/3 Building PortCore for Unity..."
"$SCRIPT_DIR/sync-portcore.sh"

if [[ -n "$PRIVATE_VOXEL_JSON" ]]; then
  if [[ ! -f "$PRIVATE_VOXEL_JSON" ]]; then
    echo "Private voxel JSON not found: $PRIVATE_VOXEL_JSON"
    exit 1
  fi

  OUTPUT_CATALOG="$PROJECT_PATH/Assets/TimeCli/PrivateGenerated/Resources/TimeCliVoxelCatalog.bytes"

  echo "2/3 Building private canonical voxel catalog..."
  python3 "$SCRIPT_DIR/build-private-voxel-catalog.py"     "$PRIVATE_VOXEL_JSON"     "$OUTPUT_CATALOG"
else
  echo "2/3 No private voxel JSON supplied; Unity will use the development catalog."
fi

echo "3/3 Running Unity batch compile + Arena scene generation..."
"$UNITY_PATH"   -batchmode   -nographics   -quit   -projectPath "$PROJECT_PATH"   -executeMethod TimeCli.UnityRuntime.Editor.TimeCliProjectSetup.CreateArenaPrototypeScene   -logFile "$LOG_PATH"

echo
echo "Arena prototype generated successfully."
echo "Scene:"
echo "  $PROJECT_PATH/Assets/TimeCli/Scenes/ArenaPrototype.unity"
echo "Log:"
echo "  $LOG_PATH"
