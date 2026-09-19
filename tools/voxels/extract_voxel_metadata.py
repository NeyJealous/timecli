#!/usr/bin/env python3
"""
Extract Time Clickers/Qubicle voxel metadata without exporting the original
voxel layouts into the public repository.

Input directory is expected to contain Unity 5.4 TextAsset container files.
The script emits only model-level metadata/counts by default. Use
--include-layout only for a private/local reconstruction workspace.
"""
from __future__ import annotations

import argparse
import json
import re
import struct
from pathlib import Path


RGBA_RED = (255, 0, 0, 255)
RGBA_WHITE = (255, 255, 255, 255)
RGBA_BLACK = (0, 0, 0, 255)
RGBA_BLUE = (0, 0, 255, 255)


def parse_unity_textasset_container(path: Path) -> tuple[str, bytes]:
    b = path.read_bytes()
    if len(b) < 32:
        raise ValueError("file too small")

    metadata_size, file_size, version, data_offset = struct.unpack_from(">4I", b, 0)
    if version < 14:
        raise ValueError(f"unsupported serialized version {version}")

    endian = b[16]
    order = "<" if endian == 0 else ">"
    pos = 20

    end = b.index(0, pos)
    engine = b[pos:end].decode("utf-8", "replace")
    pos = end + 1
    pos += 4  # target platform
    enable_type_tree = b[pos]
    pos += 1
    if enable_type_tree:
        raise ValueError("type-tree files are not supported")

    ntypes = struct.unpack_from(order + "i", b, pos)[0]
    pos += 4
    types = []

    for _ in range(ntypes):
        class_id = struct.unpack_from(order + "i", b, pos)[0]
        pos += 4
        pos += 1  # stripped
        pos += 2  # script index
        if class_id == 114:
            pos += 16
        pos += 16
        types.append(class_id)

    nobj = struct.unpack_from(order + "i", b, pos)[0]
    pos += 4
    text_objects = []

    for _ in range(nobj):
        pos = (pos + 3) & ~3
        path_id = struct.unpack_from(order + "q", b, pos)[0]
        pos += 8
        byte_start = struct.unpack_from(order + "I", b, pos)[0] + data_offset
        pos += 4
        byte_size = struct.unpack_from(order + "I", b, pos)[0]
        pos += 4
        type_id = struct.unpack_from(order + "i", b, pos)[0]
        pos += 4
        pos += 2  # script type index

        if 0 <= type_id < len(types) and types[type_id] == 49:
            text_objects.append((path_id, byte_start, byte_size))

    if len(text_objects) != 1:
        raise ValueError(f"expected one TextAsset, got {len(text_objects)}")

    _, start, size = text_objects[0]
    p = start

    name_len = struct.unpack_from(order + "i", b, p)[0]
    p += 4
    name = b[p:p + name_len].decode("utf-8", "replace")
    p += name_len
    p = (p + 3) & ~3

    data_len = struct.unpack_from(order + "i", b, p)[0]
    p += 4
    data = b[p:p + data_len]

    return name, data


def parse_qb(data: bytes, include_layout: bool = False) -> dict:
    version, color_format, z_axis, compressed, visibility, matrix_count = (
        struct.unpack_from("<6I", data, 0)
    )
    if compressed != 0:
        raise ValueError("compressed QB is not used by the 1.4.5 corpus")

    pos = 24
    matrices = []

    for _ in range(matrix_count):
        name_len = data[pos]
        pos += 1
        name = data[pos:pos + name_len].decode("utf-8", "replace")
        pos += name_len

        sx, sy, sz = struct.unpack_from("<3I", data, pos)
        pos += 12
        px, py, pz = struct.unpack_from("<3i", data, pos)
        pos += 12

        counts = {"red": 0, "white": 0, "black": 0, "blue": 0, "other": 0}
        layout = []

        for z in range(sz):
            for y in range(sy):
                for x in range(sx):
                    rgba = tuple(data[pos:pos + 4])
                    pos += 4
                    if rgba == (0, 0, 0, 0):
                        continue

                    key = {
                        RGBA_RED: "red",
                        RGBA_WHITE: "white",
                        RGBA_BLACK: "black",
                        RGBA_BLUE: "blue",
                    }.get(rgba, "other")
                    counts[key] += 1

                    if include_layout:
                        layout.append([x, y, z, key])

        item = {
            "name": name,
            "size": [sx, sy, sz],
            "position": [px, py, pz],
            "counts": counts,
        }
        if include_layout:
            item["layout"] = layout
        matrices.append(item)

    if pos != len(data):
        raise ValueError(f"unexpected trailing bytes: {len(data) - pos}")

    return {
        "version": version,
        "colorFormat": color_format,
        "zAxis": z_axis,
        "visibility": visibility,
        "matrices": matrices,
    }


def min_wave(name: str) -> int:
    for prefix, wave in (
        ("a_", 100),
        ("b_", 250),
        ("c_", 500),
        ("d_", 1000),
        ("e_", 2000),
        ("f_", 4000),
    ):
        if name.startswith(prefix):
            return wave

    m = re.match(r"^(\d+)_", name)
    if not m:
        return 0

    wave = int(m.group(1))
    return 9_999_999 if wave == 666 else wave


def boss_wave(name: str) -> int | None:
    m = re.match(r"^(\d+)_", name)
    return int(m.group(1)) if m else None


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("input_dir", type=Path)
    ap.add_argument("output_json", type=Path)
    ap.add_argument("--include-layout", action="store_true")
    args = ap.parse_args()

    rows = []

    for path in sorted(args.input_dir.iterdir()):
        if not path.is_file() or ".split" in path.name:
            continue

        try:
            asset_name, qb = parse_unity_textasset_container(path)
            parsed = parse_qb(qb, include_layout=args.include_layout)
        except Exception:
            continue

        for matrix in parsed["matrices"]:
            counts = matrix["counts"]
            row = {
                "asset": asset_name,
                "matrix": matrix["name"],
                "minWave": min_wave(asset_name),
                "bossWave": boss_wave(asset_name),
                "size": matrix["size"],
                "position": matrix["position"],
                "enemyCount": sum(counts.values()),
                **counts,
            }
            if args.include_layout:
                row["layout"] = matrix["layout"]
            rows.append(row)

    args.output_json.parent.mkdir(parents=True, exist_ok=True)
    args.output_json.write_text(
        json.dumps(rows, indent=2),
        encoding="utf-8",
    )

    print(f"Extracted {len(rows)} voxel models -> {args.output_json}")


if __name__ == "__main__":
    main()
