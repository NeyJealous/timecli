#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT_PATH="$REPO_ROOT/unity/TimeCli"
LOG_DIR="$REPO_ROOT/out/unity"
LOG_PATH="$LOG_DIR/arena-prototype-batch.log"

UNITY_PATH="${UNITY_PATH:-}"
PRIVATE_VOXEL_JSON="${PRIVATE_VOXEL_JSON:-}"
ORIGINAL_DATA_SOURCE="${ORIGINAL_DATA_SOURCE:-}"
REQUIRE_WEAPON_HIERARCHY="${REQUIRE_WEAPON_HIERARCHY:-0}"

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

echo "1/5 Building PortCore for Unity..."
"$SCRIPT_DIR/sync-portcore.sh"

if [[ -n "$PRIVATE_VOXEL_JSON" ]]; then
  if [[ ! -f "$PRIVATE_VOXEL_JSON" ]]; then
    echo "Private voxel JSON not found: $PRIVATE_VOXEL_JSON"
    exit 1
  fi

  OUTPUT_CATALOG="$PROJECT_PATH/Assets/TimeCli/PrivateGenerated/Resources/TimeCliVoxelCatalog.bytes"

  echo "2/5 Building private canonical voxel catalog..."
  python3 "$SCRIPT_DIR/build-private-voxel-catalog.py"     "$PRIVATE_VOXEL_JSON"     "$OUTPUT_CATALOG"
else
  echo "2/5 No private voxel JSON supplied; Unity will use the development catalog."
fi

if [[ -n "$ORIGINAL_DATA_SOURCE" ]]; then
  if [[ ! -e "$ORIGINAL_DATA_SOURCE" ]]; then
    echo "Original Data/sharedassets source not found: $ORIGINAL_DATA_SOURCE"
    exit 1
  fi

  PRIVATE_RESOURCES="$PROJECT_PATH/Assets/TimeCli/PrivateGenerated/Resources"

  echo "3/5 Extracting private TimeCube/WeaponCube textures..."
  python3 "$SCRIPT_DIR/extract-private-special-textures.py"     "$ORIGINAL_DATA_SOURCE"     "$PRIVATE_RESOURCES"
else
  echo "3/5 No original Data source supplied; special cubes use procedural fallback visuals."
fi

if [[ -n "$ORIGINAL_DATA_SOURCE" ]]; then
  HIERARCHY_OUTPUT="$PROJECT_PATH/Assets/TimeCli/PrivateGenerated/weapon_hierarchy.json"

  if python3 -c "import UnityPy" >/dev/null 2>&1; then
    echo "4/5 Extracting and validating private click-weapon hierarchy..."
    python3 "$SCRIPT_DIR/extract-private-weapon-hierarchy.py" \
      "$ORIGINAL_DATA_SOURCE" \
      "$HIERARCHY_OUTPUT"
    python3 "$SCRIPT_DIR/validate-private-weapon-hierarchy.py" \
      "$HIERARCHY_OUTPUT"
  elif [[ "$REQUIRE_WEAPON_HIERARCHY" == "1" ]]; then
    echo "UnityPy is required for exact click-weapon hierarchy recovery."
    echo "Install it with: python3 -m pip install UnityPy"
    exit 1
  else
    echo "4/5 UnityPy is not installed; click-weapon hierarchy extraction skipped."
    echo "Install it with: python3 -m pip install UnityPy"
  fi
else
  if [[ "$REQUIRE_WEAPON_HIERARCHY" == "1" ]]; then
    echo "REQUIRE_WEAPON_HIERARCHY=1 requires ORIGINAL_DATA_SOURCE."
    exit 1
  fi

  echo "4/5 No original Data source supplied; click-weapon hierarchy extraction skipped."
fi

echo "5/5 Running Unity batch compile + Arena scene generation..."
"$UNITY_PATH"   -batchmode   -nographics   -quit   -projectPath "$PROJECT_PATH"   -executeMethod TimeCli.UnityRuntime.Editor.TimeCliProjectSetup.CreateArenaPrototypeScene   -logFile "$LOG_PATH"

echo
echo "Arena prototype generated successfully."
echo "Scene:"
echo "  $PROJECT_PATH/Assets/TimeCli/Scenes/ArenaPrototype.unity"
echo "Log:"
echo "  $LOG_PATH"
