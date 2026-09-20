#!/usr/bin/env python3
"""Extract original Time Clickers 1.4.5 click-weapon transform hierarchy.

The tool itself is safe for the public reconstruction repository. Its JSON
output is original-derived and should remain in a private/ignored workspace.

Requires:
    python -m pip install UnityPy

Input may be either one serialized Unity scene file or the APK assets/bin/Data
folder containing levelN or levelN.split0..N files.
"""
from __future__ import annotations

import argparse
import io
import json
import re
from collections import deque
from pathlib import Path
from typing import Any, Iterable


DEFAULT_TARGETS = (
    "ClickerPistol",
    "ClickPistol",
    "ClickCannon",
    "ClickLauncher",
    "HeroWeapons",
)


def _import_unitypy():
    try:
        import UnityPy  # type: ignore
    except ImportError as exc:
        raise SystemExit(
            "UnityPy is required. Install it with: "
            "python -m pip install UnityPy"
        ) from exc
    return UnityPy


def _split_index(path: Path) -> int:
    match = re.search(r"\.split(\d+)$", path.name)
    return int(match.group(1)) if match else -1


def _scene_sources(source: Path) -> Iterable[tuple[str, bytes]]:
    if source.is_file():
        yield source.name, source.read_bytes()
        return

    if not source.is_dir():
        raise SystemExit(f"Input does not exist: {source}")

    groups: dict[str, list[Path]] = {}
    for path in source.iterdir():
        if not path.is_file():
            continue

        match = re.fullmatch(r"(level\d+)(?:\.split(\d+))?", path.name)
        if match is None:
            continue

        groups.setdefault(match.group(1), []).append(path)

    if not groups:
        raise SystemExit(
            "No Unity scene candidates found. Expected levelN or "
            "levelN.split0..N files."
        )

    def level_number(name: str) -> int:
        return int(name.removeprefix("level"))

    for base_name in sorted(groups, key=level_number):
        parts = groups[base_name]
        whole = [p for p in parts if ".split" not in p.name]

        if whole:
            yield base_name, whole[0].read_bytes()
            continue

        ordered = sorted(parts, key=_split_index)
        expected = list(range(len(ordered)))
        actual = [_split_index(p) for p in ordered]
        if actual != expected:
            raise SystemExit(
                f"{base_name}: split parts are not contiguous: {actual}"
            )

        yield base_name, b"".join(p.read_bytes() for p in ordered)


def _path_id(value: Any) -> int:
    if value is None:
        return 0

    if isinstance(value, dict):
        for key in ("m_PathID", "path_id", "pathID"):
            if key in value:
                return int(value[key])
        return 0

    for name in ("path_id", "m_PathID", "pathID"):
        if hasattr(value, name):
            return int(getattr(value, name))

    return 0


def _component_ptr(entry: Any) -> int:
    if isinstance(entry, dict):
        for key in ("component", "m_Component"):
            if key in entry:
                return _path_id(entry[key])
        return _path_id(entry)
    return _path_id(entry)


def _vec3(value: Any) -> list[float]:
    if not isinstance(value, dict):
        return [0.0, 0.0, 0.0]
    return [
        float(value.get("x", 0.0)),
        float(value.get("y", 0.0)),
        float(value.get("z", 0.0)),
    ]


def _quat(value: Any) -> list[float]:
    if not isinstance(value, dict):
        return [0.0, 0.0, 0.0, 1.0]
    return [
        float(value.get("x", 0.0)),
        float(value.get("y", 0.0)),
        float(value.get("z", 0.0)),
        float(value.get("w", 1.0)),
    ]


def _read_tree(obj: Any) -> dict[str, Any] | None:
    try:
        tree = obj.read_typetree()
    except Exception:
        return None
    return tree if isinstance(tree, dict) else None


def _extract_scene(
    scene_name: str,
    data: bytes,
    target_names: set[str],
    descendant_depth: int,
) -> dict[str, Any] | None:
    UnityPy = _import_unitypy()

    try:
        env = UnityPy.load(io.BytesIO(data))
    except Exception as exc:
        raise RuntimeError(f"{scene_name}: UnityPy failed to load scene") from exc

    objects = list(env.objects)
    type_by_path = {int(obj.path_id): obj.type.name for obj in objects}

    game_objects: dict[int, dict[str, Any]] = {}
    transforms: dict[int, dict[str, Any]] = {}

    for obj in objects:
        type_name = obj.type.name
        if type_name not in {"GameObject", "Transform", "RectTransform"}:
            continue

        tree = _read_tree(obj)
        if tree is None:
            continue

        path_id = int(obj.path_id)

        if type_name == "GameObject":
            components = [
                _component_ptr(entry)
                for entry in tree.get("m_Component", [])
            ]
            components = [pid for pid in components if pid]
            transform_id = next(
                (
                    pid
                    for pid in components
                    if type_by_path.get(pid) in {"Transform", "RectTransform"}
                ),
                0,
            )

            game_objects[path_id] = {
                "pathId": path_id,
                "name": str(tree.get("m_Name", "")),
                "layer": int(tree.get("m_Layer", 0)),
                "active": bool(tree.get("m_IsActive", 1)),
                "transformPathId": transform_id,
                "componentPathIds": components,
                "componentTypes": [
                    type_by_path.get(pid, "Unknown") for pid in components
                ],
            }
            continue

        game_object_id = _path_id(tree.get("m_GameObject"))
        children = [
            _path_id(child)
            for child in tree.get("m_Children", [])
        ]
        children = [pid for pid in children if pid]

        transforms[path_id] = {
            "pathId": path_id,
            "gameObjectPathId": game_object_id,
            "parentTransformPathId": _path_id(tree.get("m_Father")),
            "childTransformPathIds": children,
            "rootOrder": int(tree.get("m_RootOrder", 0)),
            "localPosition": _vec3(tree.get("m_LocalPosition")),
            "localRotationQuaternion": _quat(tree.get("m_LocalRotation")),
            "localScale": _vec3(tree.get("m_LocalScale")),
        }

    go_by_transform = {
        go["transformPathId"]: go
        for go in game_objects.values()
        if go["transformPathId"]
    }

    exact_targets = {
        transform_id
        for transform_id, go in go_by_transform.items()
        if go["name"] in target_names
    }
    if not exact_targets:
        return None

    included: set[int] = set()

    # Preserve every ancestor so the authored parent chain is reproducible.
    for target_id in exact_targets:
        current = target_id
        seen: set[int] = set()
        while current and current not in seen:
            seen.add(current)
            included.add(current)
            current = transforms.get(current, {}).get(
                "parentTransformPathId", 0
            )

    # Preserve nearby descendants such as Firespot / visual / aim pivots
    # without dumping the complete original Arena scene.
    queue: deque[tuple[int, int]] = deque(
        (target_id, 0) for target_id in exact_targets
    )
    while queue:
        current, depth = queue.popleft()
        included.add(current)
        if depth >= descendant_depth:
            continue
        for child in transforms.get(current, {}).get(
            "childTransformPathIds", []
        ):
            queue.append((child, depth + 1))

    def hierarchy_path(transform_id: int) -> str:
        names: list[str] = []
        current = transform_id
        seen: set[int] = set()

        while current and current not in seen:
            seen.add(current)
            go = go_by_transform.get(current)
            names.append(go["name"] if go else f"Transform#{current}")
            current = transforms.get(current, {}).get(
                "parentTransformPathId", 0
            )

        return "/".join(reversed(names))

    nodes = []
    for transform_id in included:
        transform = transforms.get(transform_id)
        if transform is None:
            continue

        go = go_by_transform.get(transform_id)
        nodes.append(
            {
                "name": go["name"] if go else "",
                "hierarchyPath": hierarchy_path(transform_id),
                "isRequestedTarget": transform_id in exact_targets,
                "gameObjectPathId": (
                    go["pathId"] if go else transform["gameObjectPathId"]
                ),
                "transformPathId": transform_id,
                "parentTransformPathId": transform[
                    "parentTransformPathId"
                ],
                "childTransformPathIds": transform[
                    "childTransformPathIds"
                ],
                "rootOrder": transform["rootOrder"],
                "localPosition": transform["localPosition"],
                "localRotationQuaternion": transform[
                    "localRotationQuaternion"
                ],
                "localScale": transform["localScale"],
                "layer": go["layer"] if go else None,
                "active": go["active"] if go else None,
                "componentPathIds": (
                    go["componentPathIds"] if go else []
                ),
                "componentTypes": go["componentTypes"] if go else [],
            }
        )

    nodes.sort(key=lambda row: row["hierarchyPath"])

    return {
        "scene": scene_name,
        "requestedNames": sorted(target_names),
        "targetMatches": [
            row["hierarchyPath"]
            for row in nodes
            if row["isRequestedTarget"]
        ],
        "nodes": nodes,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "source",
        type=Path,
        help="serialized scene file or APK assets/bin/Data directory",
    )
    parser.add_argument(
        "output_json",
        type=Path,
        help="private JSON report path",
    )
    parser.add_argument(
        "--target",
        action="append",
        dest="targets",
        help="exact GameObject name; repeat to add targets",
    )
    parser.add_argument(
        "--descendant-depth",
        type=int,
        default=6,
        help="number of child levels to retain below each matched target",
    )
    args = parser.parse_args()

    if args.descendant_depth < 0:
        raise SystemExit("--descendant-depth must be >= 0")

    target_names = set(args.targets or DEFAULT_TARGETS)
    matched_scenes = []
    failures = []

    for scene_name, data in _scene_sources(args.source):
        try:
            report = _extract_scene(
                scene_name,
                data,
                target_names,
                args.descendant_depth,
            )
        except Exception as exc:
            failures.append(f"{scene_name}: {exc}")
            continue

        if report is not None:
            matched_scenes.append(report)

    if not matched_scenes:
        details = "\n".join(failures)
        suffix = f"\nLoad failures:\n{details}" if details else ""
        raise SystemExit(
            "No requested click-weapon GameObjects were found in the supplied "
            f"scene data.{suffix}"
        )

    output = {
        "format": "timecli-weapon-hierarchy-v1",
        "originalDerived": True,
        "source": str(args.source),
        "scenes": matched_scenes,
    }

    args.output_json.parent.mkdir(parents=True, exist_ok=True)
    args.output_json.write_text(
        json.dumps(output, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )

    total_targets = sum(
        len(scene["targetMatches"]) for scene in matched_scenes
    )
    print(
        f"Recovered {total_targets} target hierarchy matches across "
        f"{len(matched_scenes)} scene(s) -> {args.output_json}"
    )


if __name__ == "__main__":
    main()
