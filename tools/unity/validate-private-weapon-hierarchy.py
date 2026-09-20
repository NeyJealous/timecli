#!/usr/bin/env python3
"""Validate private click-weapon hierarchy reports produced by the extractor."""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Any


PISTOL_NAMES = {"ClickerPistol", "ClickPistol"}
REQUIRED_GROUPS = (
    ("pistol", PISTOL_NAMES),
    ("cannon", {"ClickCannon"}),
    ("launcher", {"ClickLauncher"}),
)


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def _number_list(
    value: Any,
    length: int,
    label: str,
) -> list[float]:
    _require(isinstance(value, list), f"{label}: expected list")
    _require(len(value) == length, f"{label}: expected {length} values")

    result = []
    for index, raw in enumerate(value):
        _require(
            isinstance(raw, (int, float)) and not isinstance(raw, bool),
            f"{label}[{index}]: expected number",
        )
        number = float(raw)
        _require(math.isfinite(number), f"{label}[{index}]: non-finite value")
        result.append(number)

    return result


def _validate_scene(scene: dict[str, Any]) -> dict[str, Any]:
    name = str(scene.get("scene", ""))
    _require(name, "scene: missing name")

    nodes = scene.get("nodes")
    _require(isinstance(nodes, list) and nodes, f"{name}: no nodes")

    by_transform: dict[int, dict[str, Any]] = {}
    target_names: set[str] = set()

    for node in nodes:
        _require(isinstance(node, dict), f"{name}: node is not an object")

        transform_id = node.get("transformPathId")
        _require(
            isinstance(transform_id, int) and transform_id != 0,
            f"{name}: invalid transformPathId",
        )
        _require(
            transform_id not in by_transform,
            f"{name}: duplicate transformPathId {transform_id}",
        )

        node_name = str(node.get("name", ""))
        hierarchy_path = str(node.get("hierarchyPath", ""))
        _require(hierarchy_path, f"{name}:{transform_id}: empty hierarchyPath")

        _number_list(
            node.get("localPosition"),
            3,
            f"{name}:{hierarchy_path}:localPosition",
        )
        _number_list(
            node.get("localRotationQuaternion"),
            4,
            f"{name}:{hierarchy_path}:localRotationQuaternion",
        )
        _number_list(
            node.get("localScale"),
            3,
            f"{name}:{hierarchy_path}:localScale",
        )

        if node.get("isRequestedTarget") is True:
            target_names.add(node_name)

        by_transform[transform_id] = node

    # Every retained parent must either be present or be a true serialized root.
    for transform_id, node in by_transform.items():
        parent = node.get("parentTransformPathId", 0)
        _require(isinstance(parent, int), f"{name}:{transform_id}: invalid parent")
        if parent != 0:
            _require(
                parent in by_transform,
                f"{name}:{transform_id}: missing retained parent {parent}",
            )

    # Detect cycles in the retained parent chain.
    for transform_id in by_transform:
        seen: set[int] = set()
        current = transform_id
        while current:
            _require(
                current not in seen,
                f"{name}: transform cycle at {current}",
            )
            seen.add(current)
            current = int(
                by_transform.get(current, {}).get("parentTransformPathId", 0)
            )

    fire_candidates = []
    for node in nodes:
        node_name = str(node.get("name", ""))
        lowered = node_name.lower()
        if (
            ("fire" in lowered and "spot" in lowered)
            or "muzzle" in lowered
            or "barrel" in lowered
        ):
            fire_candidates.append(str(node.get("hierarchyPath", "")))

    return {
        "scene": name,
        "targetNames": sorted(target_names),
        "fireCandidates": sorted(set(fire_candidates)),
    }


def validate_report(report: dict[str, Any]) -> list[dict[str, Any]]:
    _require(
        report.get("format") == "timecli-weapon-hierarchy-v1",
        "unexpected report format",
    )
    _require(
        report.get("originalDerived") is True,
        "report must be marked originalDerived=true",
    )

    scenes = report.get("scenes")
    _require(isinstance(scenes, list) and scenes, "report contains no scenes")

    summaries = [_validate_scene(scene) for scene in scenes]

    all_targets = {
        target
        for summary in summaries
        for target in summary["targetNames"]
    }

    for label, accepted_names in REQUIRED_GROUPS:
        _require(
            bool(all_targets & accepted_names),
            f"missing required {label} target: "
            + " / ".join(sorted(accepted_names)),
        )

    return summaries


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("report_json", type=Path)
    args = parser.parse_args()

    report = json.loads(args.report_json.read_text(encoding="utf-8"))
    _require(isinstance(report, dict), "top-level JSON must be an object")

    summaries = validate_report(report)

    print("Weapon hierarchy report is structurally valid.")
    for summary in summaries:
        print(
            f"- {summary['scene']}: targets="
            f"{', '.join(summary['targetNames'])}"
        )
        candidates = summary["fireCandidates"]
        if candidates:
            print("  fire/pivot candidates:")
            for path in candidates:
                print(f"    {path}")
        else:
            print(
                "  fire/pivot candidates: none identified by name; "
                "manual report review is still required"
            )


if __name__ == "__main__":
    main()
