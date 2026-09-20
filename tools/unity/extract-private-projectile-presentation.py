#!/usr/bin/env python3
"""
Extract original-derived Flak/Rocket presentation resources into the ignored
Unity PrivateGenerated/Resources directory.

Requires:
    python -m pip install UnityPy

Outputs:
    TimeCliFlakBulletMesh.txt          UnityPy OBJ text
    TimeCliRocketProjectileMesh.txt    UnityPy OBJ text
    TimeCliRocketProjectileTexture.png
    TimeCliRocketProjectileAudio.wav

The OBJ files remain private. UnityPy's OBJ exporter converts Unity's
left-handed mesh into right-handed OBJ by negating X and reversing winding.
The public runtime loader explicitly reverses that conversion when rebuilding
the Mesh in Unity.
"""
from __future__ import annotations

import argparse
import shutil
import tempfile
from pathlib import Path

import UnityPy
from UnityPy.export.Texture2DConverter import parse_image_data


FLAK_MESH_ID = 253
ROCKET_MESH_ID = 257
ROCKET_TEXTURE_ID = 3
ROCKET_AUDIO_ID = 407

FLAK_MESH_NAME = "Flak Bullet"
ROCKET_AUDIO_NAME = "RocketLauncherFire MOD"

CUBEMAP_FACE_NAMES = (
    "PositiveX",
    "NegativeX",
    "PositiveY",
    "NegativeY",
    "PositiveZ",
    "NegativeZ",
)


def gather_split_parts(data_dir: Path, base: str) -> list[Path]:
    prefix = base + ".split"
    parts: list[tuple[int, Path]] = []

    for path in data_dir.iterdir():
        if not path.name.startswith(prefix):
            continue
        suffix = path.name[len(prefix):]
        if suffix.isdigit():
            parts.append((int(suffix), path))

    parts.sort(key=lambda item: item[0])

    if parts:
        actual = [index for index, _ in parts]
        expected = list(range(len(parts)))
        if actual != expected:
            raise RuntimeError(
                f"{base}: expected contiguous split indices "
                f"{expected}, got {actual}"
            )

    return [path for _, path in parts]


def materialize(
    data_dir: Path,
    base: str,
    staging: Path,
    required: bool = True,
) -> Path | None:
    direct = data_dir / base
    if direct.is_file():
        target = staging / base
        shutil.copyfile(direct, target)
        return target

    parts = gather_split_parts(data_dir, base)
    if not parts:
        if required:
            raise FileNotFoundError(
                f"Could not find {base} or {base}.splitN under {data_dir}"
            )
        return None

    target = staging / base
    with target.open("wb") as dst:
        for part in parts:
            with part.open("rb") as src:
                shutil.copyfileobj(src, dst)

    return target


def resolve_data_dir(source: Path) -> Path:
    if source.is_dir():
        return source
    if source.is_file():
        return source.parent
    raise FileNotFoundError(source)


def find_object(env, type_name: str, path_id: int):
    obj = next(
        (
            obj for obj in env.objects
            if obj.type.name == type_name
            and int(obj.path_id) == path_id
        ),
        None,
    )
    if obj is None:
        raise RuntimeError(
            f"{type_name} pathID {path_id} was not found"
        )
    return obj


def write_obj(env, path_id: int, expected_name: str | None, target: Path) -> None:
    obj = find_object(env, "Mesh", path_id)
    mesh = obj.parse_as_object()

    if expected_name is not None and mesh.m_Name != expected_name:
        raise RuntimeError(
            f"Mesh pathID {path_id}: expected {expected_name!r}, "
            f"got {mesh.m_Name!r}"
        )

    exported = mesh.export("obj")
    if not exported:
        raise RuntimeError(
            f"Mesh pathID {path_id}: UnityPy returned no OBJ data"
        )

    target.write_text(exported, encoding="utf-8")

    vertices = sum(
        1 for line in exported.splitlines()
        if line.startswith("v ")
    )
    faces = sum(
        1 for line in exported.splitlines()
        if line.startswith("f ")
    )
    print(
        f"{target.name}: pathID={path_id} "
        f"name={mesh.m_Name!r} vertices={vertices} faces={faces}"
    )


def write_texture(env, target: Path) -> None:
    obj = find_object(env, "Texture2D", ROCKET_TEXTURE_ID)
    tex = obj.parse_as_object()

    if tex.m_Name != "tgarocket MOD ACID":
        raise RuntimeError(
            f"Texture pathID {ROCKET_TEXTURE_ID}: "
            f"unexpected name {tex.m_Name!r}"
        )

    image = tex.image
    if image is None:
        raise RuntimeError("UnityPy returned no decoded Rocket texture")

    image.save(target, format="PNG")
    print(
        f"{target.name}: pathID={ROCKET_TEXTURE_ID} "
        f"name={tex.m_Name!r} size={image.width}x{image.height}"
    )


def write_audio(env, target: Path) -> None:
    obj = find_object(env, "AudioClip", ROCKET_AUDIO_ID)
    clip = obj.parse_as_object()

    if clip.m_Name != ROCKET_AUDIO_NAME:
        raise RuntimeError(
            f"Audio pathID {ROCKET_AUDIO_ID}: expected "
            f"{ROCKET_AUDIO_NAME!r}, got {clip.m_Name!r}"
        )

    samples = clip.samples
    if not samples:
        raise RuntimeError("UnityPy returned no Rocket audio samples")

    wav = next(iter(samples.values()))
    if not wav.startswith(b"RIFF"):
        raise RuntimeError("Expected UnityPy Rocket audio export to be WAV/RIFF")

    target.write_bytes(wav)
    print(
        f"{target.name}: pathID={ROCKET_AUDIO_ID} "
        f"name={clip.m_Name!r} bytes={len(wav)}"
    )


def write_cubemap_faces(
    env,
    path_id: int,
    expected_name: str,
    output_prefix: str,
    output_dir: Path,
) -> None:
    obj = find_object(env, "Cubemap", path_id)
    cube = obj.parse_as_object()

    if cube.m_Name != expected_name:
        raise RuntimeError(
            f"Cubemap pathID {path_id}: expected {expected_name!r}, "
            f"got {cube.m_Name!r}"
        )

    data = bytes(cube.image_data)
    face_bytes = int(cube.m_CompleteImageSize)

    if len(data) != face_bytes * 6:
        raise RuntimeError(
            f"Cubemap {expected_name!r}: expected {face_bytes * 6} bytes, "
            f"got {len(data)}"
        )

    for index, face_name in enumerate(CUBEMAP_FACE_NAMES):
        start = index * face_bytes
        face_data = data[start:start + face_bytes]
        image = parse_image_data(
            face_data,
            int(cube.m_Width),
            int(cube.m_Height),
            cube.m_TextureFormat,
            getattr(cube.object_reader, "version", (0, 0, 0, 0)),
            getattr(cube.object_reader, "platform", 0),
            getattr(cube, "m_PlatformBlob", None),
            True,
        )
        target = output_dir / f"{output_prefix}_{face_name}.png"
        image.save(target, format="PNG")
        print(
            f"{target.name}: cubemap pathID={path_id} "
            f"face={face_name} size={image.width}x{image.height}"
        )


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument(
        "source",
        type=Path,
        help="APK assets/bin/Data directory or a reassembled Data asset file",
    )
    ap.add_argument(
        "output_dir",
        type=Path,
        help="PrivateGenerated/Resources output directory",
    )
    args = ap.parse_args()

    data_dir = resolve_data_dir(args.source)
    args.output_dir.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory(prefix="timecli-projectiles-") as td:
        staging = Path(td)

        shared = materialize(
            data_dir,
            "sharedassets1.assets",
            staging,
        )
        materialize(
            data_dir,
            "sharedassets1.resource",
            staging,
            required=False,
        )
        globals_file = materialize(
            data_dir,
            "globalgamemanagers.assets",
            staging,
        )
        materialize(
            data_dir,
            "globalgamemanagers.resource",
            staging,
            required=False,
        )

        shared_env = UnityPy.load(str(shared))
        global_env = UnityPy.load(str(globals_file))

        write_obj(
            shared_env,
            FLAK_MESH_ID,
            FLAK_MESH_NAME,
            args.output_dir / "TimeCliFlakBulletMesh.txt",
        )
        write_obj(
            shared_env,
            ROCKET_MESH_ID,
            None,
            args.output_dir / "TimeCliRocketProjectileMesh.txt",
        )
        write_texture(
            global_env,
            args.output_dir / "TimeCliRocketProjectileTexture.png",
        )
        write_audio(
            shared_env,
            args.output_dir / "TimeCliRocketProjectileAudio.wav",
        )
        write_cubemap_faces(
            shared_env,
            427,
            "Channel_Cubemap",
            "TimeCliChannelCubemap",
            args.output_dir,
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
