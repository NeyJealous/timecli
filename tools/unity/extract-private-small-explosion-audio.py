#!/usr/bin/env python3
"""Extract the canonical SmallExplosion impact clip into ignored Unity Resources.

The original clip remains private/original-derived. This tool reads it from a
local Time Clickers 1.4.5 Data directory (including split containers) and writes
only to the caller-selected private output directory.
"""

from __future__ import annotations

import argparse
import shutil
import tempfile
from pathlib import Path


CLIP_PATH_ID = 410
CLIP_NAME = "Misc_MechAbstract_Impact_02"
OUTPUT_NAME = "TimeCliSmallExplosionImpact.wav"


def gather_split_parts(data_dir: Path, base: str) -> list[Path]:
    parts: list[tuple[int, Path]] = []
    prefix = base + ".split"

    for path in data_dir.iterdir():
        if not path.name.startswith(prefix):
            continue
        suffix = path.name[len(prefix):]
        if suffix.isdigit():
            parts.append((int(suffix), path))

    parts.sort(key=lambda item: item[0])

    if not parts:
        return []

    expected = list(range(len(parts)))
    actual = [index for index, _ in parts]
    if actual != expected:
        raise RuntimeError(
            f"{base}: expected contiguous split indices {expected}, got {actual}"
        )

    return [path for _, path in parts]


def materialize(data_dir: Path, base: str, staging: Path) -> Path:
    direct = data_dir / base
    if direct.is_file():
        target = staging / base
        shutil.copyfile(direct, target)
        return target

    parts = gather_split_parts(data_dir, base)
    if not parts:
        raise FileNotFoundError(
            f"Could not find {base} or {base}.splitN under {data_dir}"
        )

    target = staging / base
    with target.open("wb") as dst:
        for part in parts:
            with part.open("rb") as src:
                shutil.copyfileobj(src, dst)

    return target


def resolve_data_dir(source: Path) -> Path:
    if source.is_dir():
        return source

    if source.name == "sharedassets1.assets":
        return source.parent

    raise ValueError(
        "SOURCE must be the APK assets/bin/Data directory or a "
        "sharedassets1.assets file whose resource file is beside it."
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "source",
        type=Path,
        help="APK assets/bin/Data directory or sharedassets1.assets",
    )
    parser.add_argument(
        "output_dir",
        type=Path,
        help="PrivateGenerated/Resources output directory",
    )
    args = parser.parse_args()

    try:
        import UnityPy
    except ImportError as exc:
        raise SystemExit(
            "UnityPy is required for AudioClip conversion. "
            "Install it with: python -m pip install UnityPy"
        ) from exc

    data_dir = resolve_data_dir(args.source)
    output_dir = args.output_dir
    output_dir.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory(prefix="timecli-audio-") as temp:
        staging = Path(temp)
        asset_path = materialize(
            data_dir,
            "sharedassets1.assets",
            staging,
        )
        materialize(
            data_dir,
            "sharedassets1.resource",
            staging,
        )

        env = UnityPy.load(str(asset_path))

        reader = next(
            (
                obj for obj in env.objects
                if obj.type.name == "AudioClip"
                and int(obj.path_id) == CLIP_PATH_ID
            ),
            None,
        )

        if reader is None:
            raise RuntimeError(
                f"AudioClip pathID {CLIP_PATH_ID} was not found."
            )

        clip = reader.parse_as_object()
        if clip.m_Name != CLIP_NAME:
            raise RuntimeError(
                f"Expected {CLIP_NAME!r}, got {clip.m_Name!r}."
            )

        samples = clip.samples
        if not samples:
            raise RuntimeError("UnityPy returned no decoded samples.")

        # The canonical clip has one subsound. UnityPy converts FMOD data to WAV.
        sample_name, sample_bytes = next(iter(samples.items()))
        if not sample_bytes.startswith(b"RIFF"):
            raise RuntimeError(
                f"Expected WAV/RIFF output, got sample {sample_name!r}."
            )

        target = output_dir / OUTPUT_NAME
        target.write_bytes(sample_bytes)

        print(
            f"Extracted {CLIP_NAME} pathID={CLIP_PATH_ID} "
            f"-> {target} ({len(sample_bytes)} bytes)"
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
