#!/usr/bin/env python3
"""Extract canonical click-weapon firing clips to ignored Unity Resources."""
from __future__ import annotations

import argparse
import shutil
import tempfile
from pathlib import Path

import UnityPy

CLIPS = {
    397: ("pistol1", "TimeCliClickPistolFire.wav"),
    396: ("MechWeapons_Shock_Fire_02", "TimeCliClickCannonFire.wav"),
    400: ("MechWeapons_Grenade_Fire_01", "TimeCliClickLauncherFire.wav"),
}

def gather_split_parts(data_dir: Path, base: str) -> list[Path]:
    prefix=base+".split"
    parts=[]
    for path in data_dir.iterdir():
        if path.name.startswith(prefix):
            suffix=path.name[len(prefix):]
            if suffix.isdigit():
                parts.append((int(suffix),path))
    parts.sort(key=lambda x:x[0])
    if parts and [i for i,_ in parts] != list(range(len(parts))):
        raise RuntimeError(f"{base}: split parts are not contiguous")
    return [p for _,p in parts]

def materialize(data_dir: Path, base: str, staging: Path, required=True) -> Path | None:
    direct=data_dir/base
    if direct.is_file():
        target=staging/base
        shutil.copyfile(direct,target)
        return target
    parts=gather_split_parts(data_dir,base)
    if not parts:
        if required:
            raise FileNotFoundError(f"Could not find {base} under {data_dir}")
        return None
    target=staging/base
    with target.open("wb") as dst:
        for part in parts:
            with part.open("rb") as src:
                shutil.copyfileobj(src,dst)
    return target

def resolve_data_dir(source: Path) -> Path:
    if source.is_dir(): return source
    if source.is_file(): return source.parent
    raise FileNotFoundError(source)

def main() -> int:
    ap=argparse.ArgumentParser()
    ap.add_argument("source",type=Path)
    ap.add_argument("output_dir",type=Path)
    args=ap.parse_args()

    data_dir=resolve_data_dir(args.source)
    args.output_dir.mkdir(parents=True,exist_ok=True)

    with tempfile.TemporaryDirectory(prefix="timecli-click-audio-") as td:
        staging=Path(td)
        shared=materialize(data_dir,"sharedassets1.assets",staging)
        materialize(data_dir,"sharedassets1.resource",staging,required=False)
        env=UnityPy.load(str(shared))

        for path_id,(expected_name,filename) in CLIPS.items():
            reader=next(
                (o for o in env.objects
                 if o.type.name=="AudioClip" and int(o.path_id)==path_id),
                None,
            )
            if reader is None:
                raise RuntimeError(f"AudioClip pathID {path_id} not found")
            clip=reader.parse_as_object()
            if clip.m_Name != expected_name:
                raise RuntimeError(
                    f"AudioClip {path_id}: expected {expected_name!r}, got {clip.m_Name!r}"
                )
            samples=clip.samples
            if not samples:
                raise RuntimeError(f"AudioClip {path_id}: no decoded samples")
            wav=next(iter(samples.values()))
            if not wav.startswith(b"RIFF"):
                raise RuntimeError(f"AudioClip {path_id}: expected WAV/RIFF")
            target=args.output_dir/filename
            target.write_bytes(wav)
            print(f"{filename}: pathID={path_id} name={clip.m_Name!r} bytes={len(wav)}")

    return 0

if __name__=="__main__":
    raise SystemExit(main())
