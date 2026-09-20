#!/usr/bin/env python3
"""
Convert private voxel_layouts_private.json into a compact Unity Resources binary.

The generated file contains original-derived voxel coordinates and therefore
MUST stay out of the public repository.

Example:
    python tools/unity/build-private-voxel-catalog.py \
      /path/to/voxel_layouts_private.json \
      unity/TimeCli/Assets/TimeCli/PrivateGenerated/Resources/TimeCliVoxelCatalog.bytes
"""
from __future__ import annotations

import argparse
import json
import struct
from pathlib import Path


MAGIC = b"TCLIVOX1"
CATEGORIES = ("red", "white", "black", "blue")


def write_i32(f, value: int) -> None:
    f.write(struct.pack("<i", value))


def write_f32(f, value: float) -> None:
    f.write(struct.pack("<f", value))


def write_string(f, value: str) -> None:
    data = value.encode("utf-8")
    write_i32(f, len(data))
    f.write(data)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("input_json", type=Path)
    ap.add_argument("output_bytes", type=Path)
    args = ap.parse_args()

    rows = json.loads(args.input_json.read_text(encoding="utf-8"))
    if not isinstance(rows, list):
        raise SystemExit("Expected a root JSON array.")

    args.output_bytes.parent.mkdir(parents=True, exist_ok=True)

    with args.output_bytes.open("wb") as f:
        f.write(MAGIC)
        write_i32(f, len(rows))

        for row in rows:
            model_id = row["asset"]
            size = row["size"]
            occupied = row["occupied"]

            grouped: dict[str, list[list[object]]] = {
                key: [] for key in CATEGORIES
            }

            for voxel in occupied:
                x, y, z, color = voxel
                if color not in grouped:
                    raise ValueError(
                        f"{model_id}: unsupported voxel color {color!r}"
                    )
                grouped[color].append([x, y, z])

            expected = int(row["enemyCount"])
            actual = sum(len(grouped[key]) for key in CATEGORIES)
            if actual != expected:
                raise ValueError(
                    f"{model_id}: enemyCount {expected}, occupied {actual}"
                )

            write_string(f, model_id)
            write_i32(f, int(row["minWave"]))
            write_i32(
                f,
                -1 if row.get("bossWave") is None else int(row["bossWave"]),
            )

            write_f32(f, float(size[0]))
            write_f32(f, float(size[1]))
            write_f32(f, float(size[2]))

            # black is the original third Arena category, represented as
            # Yellow EnemyType in PortCore.
            for key in CATEGORIES:
                voxels = grouped[key]
                write_i32(f, len(voxels))
                for x, y, z in voxels:
                    write_f32(f, float(x))
                    write_f32(f, float(y))
                    write_f32(f, float(z))

    print(
        f"Wrote {len(rows)} private voxel models -> {args.output_bytes} "
        f"({args.output_bytes.stat().st_size} bytes)"
    )


if __name__ == "__main__":
    main()
