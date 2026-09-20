#!/usr/bin/env python3
"""
Extract private original-derived textures used by the modern TimeCli
reconstruction, including the special cubes and Rocket tail particle.

Input may be either:
  - a reassembled sharedassets1.assets file, or
  - the APK Data directory containing sharedassets1.assets.split0..N.

Outputs are PNG files intended for:
  unity/TimeCli/Assets/TimeCli/PrivateGenerated/Resources/

No original texture bytes are committed by this tool.
"""
from __future__ import annotations

import argparse
import binascii
import struct
import zlib
from pathlib import Path


def read_source(path: Path) -> bytes:
    if path.is_file():
        return path.read_bytes()

    parts = sorted(
        path.glob("sharedassets1.assets.split*"),
        key=lambda p: int(p.name.rsplit("split", 1)[1]),
    )
    if not parts:
        raise SystemExit(
            "No sharedassets1.assets file or split parts found."
        )

    return b"".join(p.read_bytes() for p in parts)


def parse_objects(data: bytes) -> dict[int, tuple[int, int, int]]:
    metadata_size, file_size, version, data_offset = struct.unpack_from(
        ">4I", data, 0
    )
    if version != 15:
        raise ValueError(f"Expected serialized-file version 15, got {version}")
    if file_size != len(data):
        raise ValueError(
            f"Serialized size mismatch: header={file_size}, actual={len(data)}"
        )

    pos = 20
    pos = data.index(0, pos) + 1  # Unity version
    pos += 4  # target platform
    pos += 1  # enable type tree

    type_count = struct.unpack_from("<i", data, pos)[0]
    pos += 4

    for _ in range(type_count):
        class_id = struct.unpack_from("<i", data, pos)[0]
        pos += 4
        if class_id < 0 or class_id == 114:
            pos += 16
        pos += 16  # old type hash

    object_count = struct.unpack_from("<i", data, pos)[0]
    pos += 4
    objects: dict[int, tuple[int, int, int]] = {}

    for _ in range(object_count):
        pos = (pos + 3) & ~3
        path_id = struct.unpack_from("<q", data, pos)[0]
        pos += 8
        byte_start = struct.unpack_from("<I", data, pos)[0] + data_offset
        pos += 4
        byte_size = struct.unpack_from("<I", data, pos)[0]
        pos += 4
        pos += 4  # type id
        class_id = struct.unpack_from("<h", data, pos)[0]
        pos += 2
        pos += 2  # script type index
        pos += 1  # stripped

        objects[path_id] = (byte_start, byte_size, class_id)

    return objects


def aligned_string(data: bytes, offset: int = 0) -> tuple[str, int]:
    length = struct.unpack_from("<i", data, offset)[0]
    offset += 4
    raw = data[offset:offset + length]
    offset += length
    offset = (offset + 3) & ~3
    return raw.decode("utf-8"), offset


def parse_texture2d(blob: bytes) -> dict:
    name, off = aligned_string(blob)

    width, height, complete_size, texture_format, mip_count = (
        struct.unpack_from("<5i", blob, off)
    )

    # Unity 5.4 Texture2D layout after mipCount:
    # isReadable byte, readAllowed byte, 2-byte alignment,
    # imageCount, textureDimension,
    # TextureSettings(filterMode, aniso, mipBias, wrapMode),
    # lightmapFormat, colorSpace, imageData byte array.
    image_len = struct.unpack_from("<i", blob, off + 56)[0]
    image_data = blob[off + 60:off + 60 + image_len]

    if len(image_data) != image_len:
        raise ValueError(f"{name}: truncated image data")

    return {
        "name": name,
        "width": width,
        "height": height,
        "complete_size": complete_size,
        "format": texture_format,
        "mip_count": mip_count,
        "data": image_data,
    }


def decode_level0(tex: dict) -> tuple[int, bytes]:
    width = tex["width"]
    height = tex["height"]
    fmt = tex["format"]
    raw = tex["data"]

    if fmt == 1:  # Alpha8
        size = width * height
        src = raw[:size]
        rgba = bytearray(size * 4)
        for i, a in enumerate(src):
            j = i * 4
            rgba[j:j + 4] = bytes((255, 255, 255, a))
        return 6, bytes(rgba)  # PNG RGBA

    if fmt == 3:  # RGB24
        size = width * height * 3
        return 2, raw[:size]  # PNG RGB

    if fmt == 4:  # RGBA32
        size = width * height * 4
        return 6, raw[:size]  # PNG RGBA

    if fmt == 7:  # RGB565
        size = width * height
        out = bytearray(size * 3)
        for i in range(size):
            v = raw[i * 2] | (raw[i * 2 + 1] << 8)
            r = ((v >> 11) & 31) * 255 // 31
            g = ((v >> 5) & 63) * 255 // 63
            b = (v & 31) * 255 // 31
            j = i * 3
            out[j:j + 3] = bytes((r, g, b))
        return 2, bytes(out)

    raise ValueError(
        f"{tex['name']}: unsupported TextureFormat {fmt}"
    )


def png_chunk(kind: bytes, payload: bytes) -> bytes:
    return (
        struct.pack(">I", len(payload))
        + kind
        + payload
        + struct.pack(
            ">I",
            binascii.crc32(kind + payload) & 0xFFFFFFFF,
        )
    )


def write_png(
    path: Path,
    width: int,
    height: int,
    color_type: int,
    pixels: bytes,
) -> None:
    channels = 4 if color_type == 6 else 3
    stride = width * channels

    # Unity raw textures use bottom-left origin. PNG uses top-left.
    rows = []
    for y in range(height - 1, -1, -1):
        row = pixels[y * stride:(y + 1) * stride]
        rows.append(b"\x00" + row)  # PNG filter type 0

    ihdr = struct.pack(
        ">IIBBBBB",
        width,
        height,
        8,
        color_type,
        0,
        0,
        0,
    )

    encoded = (
        b"\x89PNG\r\n\x1a\n"
        + png_chunk(b"IHDR", ihdr)
        + png_chunk(b"IDAT", zlib.compress(b"".join(rows), 9))
        + png_chunk(b"IEND", b"")
    )

    path.write_bytes(encoded)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument(
        "source",
        type=Path,
        help="sharedassets1.assets or APK Data directory",
    )
    ap.add_argument("output_dir", type=Path)
    args = ap.parse_args()

    data = read_source(args.source)
    objects = parse_objects(data)

    candidates = []
    for path_id, (start, size, class_id) in objects.items():
        if class_id != 28:
            continue

        tex = parse_texture2d(data[start:start + size])
        if tex["name"] in {
            "TimeCube",
            "WeaponCube",
            "Smoke Toon PRT TEX MOD",
            "FireB",
        }:
            tex["path_id"] = path_id
            candidates.append(tex)

    # Recovered 1.4.5 identities:
    # TimeCube pickup     64x64 RGB565, 7 mips
    # WeaponCube pickup   64x64 RGB24,  7 mips
    # WeaponCube block    64x64 Alpha8,  7 mips
    selected = {
        "TimeCliTimeCubePickupTexture.png": next(
            t for t in candidates
            if t["name"] == "TimeCube" and t["format"] == 7
        ),
        "TimeCliWeaponCubePickupTexture.png": next(
            t for t in candidates
            if t["name"] == "WeaponCube" and t["format"] == 3
        ),
        "TimeCliWeaponCubeTexture.png": next(
            t for t in candidates
            if t["name"] == "WeaponCube" and t["format"] == 1
        ),
        "TimeCliRocketTailTexture.png": next(
            t for t in candidates
            if t["name"] == "Smoke Toon PRT TEX MOD"
            and t["format"] == 4
        ),
        "TimeCliSmallExplosionTexture.png": next(
            t for t in candidates
            if t["name"] == "FireB"
            and t["format"] == 3
        ),
    }

    args.output_dir.mkdir(parents=True, exist_ok=True)

    for filename, tex in selected.items():
        color_type, pixels = decode_level0(tex)
        output = args.output_dir / filename
        write_png(
            output,
            tex["width"],
            tex["height"],
            color_type,
            pixels,
        )
        print(
            f"{filename}: {tex['name']} pathID={tex['path_id']} "
            f"{tex['width']}x{tex['height']} format={tex['format']} "
            f"mips={tex['mip_count']}"
        )


if __name__ == "__main__":
    main()
