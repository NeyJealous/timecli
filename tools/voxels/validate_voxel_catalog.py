#!/usr/bin/env python3
"""Validate a private extracted Time Clickers voxel catalog against the
reconstructed VoxelLibrary.GetVoxelModelForHP rules.

The input is the JSON emitted by extract_voxel_metadata.py without requiring
private layout coordinates. The validator checks model availability for every
wave in a requested range and verifies that cube-eligible waves have the
required direct White/Black(Yellow) slots.
"""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path


def arena_hp(wave: int) -> float:
    boss = wave % 5 == 0
    hp = math.ceil(
        (10.0 if boss else 1.0)
        * 10.0
        * math.pow(1.6, min(130, wave) - 1)
        + (wave - 1) * 10.0
    )

    if wave > 130:
        hp *= math.pow(1.25, min(500, wave) - 130)
        if wave > 500:
            hp *= math.pow(1.125, wave - 500)

    if hp == 26:
        hp = 30
    elif hp == 46:
        hp = 50
    return hp


def hp_plan(hp: float, minimum: int, maximum: int) -> tuple[int, int, int, int, int]:
    scaling_hp = 670 * (maximum // 40)
    exponent = max(math.floor(math.log10(hp / scaling_hp)), 1)

    if hp / math.pow(10, exponent) > 1000.0 * maximum / 40.0:
        exponent += 1

    base_hp = math.pow(10, exponent)
    red = math.floor(hp / base_hp)
    white = 0
    yellow = 0

    while red > maximum // 2:
        red -= 10
        white += 1
        if white > maximum // 2:
            white -= 10
            yellow += 1

    total = red + white + yellow

    while total < minimum and white > 0:
        white -= 1
        red += 10
        total = red + white + yellow

    while total > maximum and red >= 10:
        red -= 10
        white += 1
        total = red + white + yellow

    return red, white, yellow, total, total + 10


def candidates(models: list[dict], wave: int, total: int, upper: int) -> list[dict]:
    bosses = [m for m in models if m.get("bossWave") == wave]
    if bosses:
        return bosses

    return [
        m
        for m in models
        if m["minWave"] <= wave
        and m["enemyCount"] >= total
        and m["enemyCount"] <= upper
    ]


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("catalog", type=Path)
    ap.add_argument("--max-wave", type=int, default=5000)
    args = ap.parse_args()

    models = json.loads(args.catalog.read_text(encoding="utf-8"))
    missing = []
    missing_white = []
    missing_yellow = []
    minimum_available = None
    maximum_available = 0

    for wave in range(1, args.max_wave + 1):
        minimum = 100 if wave % 10 == 0 else 40
        maximum = minimum + 20
        red, white, yellow, total, upper = hp_plan(
            arena_hp(wave), minimum, maximum
        )
        eligible = candidates(models, wave, total, upper)

        count = len(eligible)
        minimum_available = count if minimum_available is None else min(minimum_available, count)
        maximum_available = max(maximum_available, count)

        if not eligible:
            missing.append(
                {
                    "wave": wave,
                    "required": [red, white, yellow],
                    "total": total,
                    "upper": upper,
                }
            )
            continue

        if wave >= 100 and wave % 5 == 0:
            for model in eligible:
                if model["white"] < 1:
                    missing_white.append([wave, model["asset"]])

        if wave >= 1000 and wave % 5 == 0:
            for model in eligible:
                if model["black"] < 1:
                    missing_yellow.append([wave, model["asset"]])

    result = {
        "models": len(models),
        "wavesChecked": args.max_wave,
        "minimumEligibleModels": minimum_available,
        "maximumEligibleModels": maximum_available,
        "wavesWithoutModel": missing,
        "timeCubeCandidatesWithoutWhiteSlot": missing_white,
        "weaponCubeCandidatesWithoutYellowSlot": missing_yellow,
    }

    print(json.dumps(result, indent=2))

    if missing or missing_white or missing_yellow:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
