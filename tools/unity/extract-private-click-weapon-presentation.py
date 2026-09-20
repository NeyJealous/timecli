#!/usr/bin/env python3
"""
Extract original-derived click-weapon model meshes/textures into the ignored
Unity PrivateGenerated/Resources directory.

Requires:
    python -m pip install UnityPy

Outputs:
    TimeCliPistolBaseAndGripMesh.txt
    TimeCliPistolChamberOneMesh.txt
    TimeCliPistolChamberTwoMesh.txt
    TimeCliPistolTop1Mesh.txt
    TimeCliPistolTriggerMesh.txt
    TimeCliClickCannonMesh.txt
    TimeCliClickLauncherMesh.txt
    TimeCliPistolTexture.png
    TimeCliClickCannonTexture.png
    TimeCliClickLauncherTexture.png

The public repository only contains this extraction recipe and clean runtime
loaders. Exported original-derived resources remain under ignored
PrivateGenerated/Resources.
"""
from __future__ import annotations

import argparse
import io
import shutil
import tempfile
from pathlib import Path

import UnityPy
from UnityPy.export.Texture2DConverter import parse_image_data

MESHES = {
    242: ("base_and_grip", "TimeCliPistolBaseAndGripMesh.txt"),
    261: ("chamber_one", "TimeCliPistolChamberOneMesh.txt"),
    259: ("chamber_two", "TimeCliPistolChamberTwoMesh.txt"),
    265: ("top1", "TimeCliPistolTop1Mesh.txt"),
    263: ("trigger", "TimeCliPistolTriggerMesh.txt"),
    246: ("Plane_001", "TimeCliClickCannonMesh.txt"),
    256: ("Mesh", "TimeCliClickLauncherMesh.txt"),
}

TEXTURES = {
    21: ("PulsePistol_Diff_A_WHITE", "TimeCliPistolTexture.png"),
    20: ("s20_dwith spec for unity MOD 2", "TimeCliClickCannonTexture.png"),
    16: ("tgalauncher MOD", "TimeCliClickLauncherTexture.png"),
}

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
                f"{base}: expected contiguous split indices {expected}, got {actual}"
            )
    return [path for _, path in parts]

def materialize(data_dir: Path, base: str, staging: Path, required: bool = True) -> Path | None:
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
        raise RuntimeError(f"{type_name} pathID {path_id} was not found")
    return obj

def write_obj(env, path_id: int, expected_name: str, target: Path) -> None:
    obj = find_object(env, "Mesh", path_id)
    mesh = obj.parse_as_object()
    if mesh.m_Name != expected_name:
        raise RuntimeError(
            f"Mesh pathID {path_id}: expected {expected_name!r}, got {mesh.m_Name!r}"
        )
    exported = mesh.export("obj")
    if not exported:
        raise RuntimeError(f"Mesh pathID {path_id}: UnityPy returned no OBJ data")
    target.write_text(exported, encoding="utf-8")
    vertex_count = sum(1 for line in exported.splitlines() if line.startswith("v "))
    face_count = sum(1 for line in exported.splitlines() if line.startswith("f "))
    print(
        f"{target.name}: pathID={path_id} name={mesh.m_Name!r} "
        f"vertices={vertex_count} faces={face_count}"
    )

def write_texture(env, path_id: int, expected_name: str, target: Path) -> None:
    obj = find_object(env, "Texture2D", path_id)
    tex = obj.parse_as_object()
    if tex.m_Name != expected_name:
        raise RuntimeError(
            f"Texture pathID {path_id}: expected {expected_name!r}, got {tex.m_Name!r}"
        )
    image = tex.image
    if image is None:
        raise RuntimeError(f"Texture pathID {path_id}: UnityPy returned no image")
    image.save(target, format="PNG")
    print(
        f"{target.name}: pathID={path_id} name={tex.m_Name!r} "
        f"size={image.width}x{image.height}"
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
    expected_bytes = face_bytes * 6

    if len(data) != expected_bytes:
        raise RuntimeError(
            f"Cubemap {expected_name!r}: expected {expected_bytes} bytes "
            f"(6 x {face_bytes}), got {len(data)}"
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

        target = output_dir / f"{output_prefix}_{face_name}.bytes"
        buffer = io.BytesIO()
        image.save(buffer, format="PNG")
        target.write_bytes(buffer.getvalue())
        print(
            f"{target.name}: cubemap pathID={path_id} "
            f"name={cube.m_Name!r} face={face_name} "
            f"size={image.width}x{image.height}"
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

    with tempfile.TemporaryDirectory(prefix="timecli-click-weapons-") as td:
        staging = Path(td)

        shared = materialize(data_dir, "sharedassets1.assets", staging)
        materialize(
            data_dir,
            "sharedassets1.resource",
            staging,
            required=False,
        )
        shared0 = materialize(
            data_dir,
            "sharedassets0.assets",
            staging,
        )
        materialize(
            data_dir,
            "sharedassets0.resource",
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
        shared0_env = UnityPy.load(str(shared0))
        global_env = UnityPy.load(str(globals_file))

        for path_id, (name, filename) in MESHES.items():
            write_obj(
                shared_env,
                path_id,
                name,
                args.output_dir / filename,
            )

        for path_id, (name, filename) in TEXTURES.items():
            write_texture(
                global_env,
                path_id,
                name,
                args.output_dir / filename,
            )

        write_cubemap_faces(
            shared_env,
            427,
            "Channel_Cubemap",
            "TimeCliChannelCubemap",
            args.output_dir,
        )
        write_cubemap_faces(
            shared0_env,
            61,
            "GreebleBox_Cubemap",
            "TimeCliGreebleBoxCubemap",
            args.output_dir,
        )

    return 0

if __name__ == "__main__":
    raise SystemExit(main())
