import importlib.util
import math
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


def load_script(module_name: str, relative_path: str):
    path = ROOT / relative_path
    spec = importlib.util.spec_from_file_location(module_name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


extractor = load_script(
    "timecli_weapon_extractor",
    "tools/unity/extract-private-weapon-hierarchy.py",
)
validator = load_script(
    "timecli_weapon_validator",
    "tools/unity/validate-private-weapon-hierarchy.py",
)


class WeaponHierarchyToolingTests(unittest.TestCase):
    def test_quaternion_identity_rotation(self):
        rotated = extractor._quat_rotate(
            (0.0, 0.0, 0.0, 1.0),
            (1.0, 2.0, 3.0),
        )
        self.assertEqual(rotated, (1.0, 2.0, 3.0))

    def test_quaternion_z_90_rotation(self):
        half = math.sqrt(0.5)
        rotated = extractor._quat_rotate(
            (0.0, 0.0, half, half),
            (1.0, 0.0, 0.0),
        )
        self.assertAlmostEqual(rotated[0], 0.0, places=6)
        self.assertAlmostEqual(rotated[1], 1.0, places=6)
        self.assertAlmostEqual(rotated[2], 0.0, places=6)

    def test_validator_accepts_exact_fire_spot_fixture(self):
        report = self._fixture()
        summaries = validator.validate_report(report)

        self.assertEqual(len(summaries), 1)
        verified = summaries[0]["verifiedFireSpots"]
        self.assertEqual(
            set(verified),
            {"pistol", "cannon", "launcher"},
        )

    def test_validator_rejects_far_fire_spot(self):
        report = self._fixture()
        report["scenes"][0]["fireSpotMatches"]["launcher"]["distance"] = 0.5

        with self.assertRaises(ValueError):
            validator.validate_report(report)

    @staticmethod
    def _fixture():
        definitions = [
            (
                101,
                "ClickerPistol",
                [0.600000024, 0.301000000, -7.30099994],
                "pistol",
            ),
            (
                102,
                "ClickCannon",
                [0.06999986, 0.31599993, -7.30079996],
                "cannon",
            ),
            (
                103,
                "ClickLauncher",
                [-0.59999985, 0.20599997, -7.49800003],
                "launcher",
            ),
        ]

        nodes = []
        matches = {}

        for transform_id, name, position, weapon_key in definitions:
            nodes.append(
                {
                    "name": name,
                    "hierarchyPath": name,
                    "isRequestedTarget": True,
                    "transformPathId": transform_id,
                    "parentTransformPathId": 0,
                    "localPosition": position,
                    "localRotationQuaternion": [0.0, 0.0, 0.0, 1.0],
                    "localScale": [1.0, 1.0, 1.0],
                    "worldPosition": position,
                }
            )
            matches[weapon_key] = {
                "transformPathId": transform_id,
                "hierarchyPath": name,
                "worldPosition": position,
                "expectedWorldPosition": position,
                "distance": 0.0,
            }

        return {
            "format": "timecli-weapon-hierarchy-v1",
            "originalDerived": True,
            "scenes": [
                {
                    "scene": "fixture",
                    "nodes": nodes,
                    "fireSpotMatches": matches,
                }
            ],
        }


if __name__ == "__main__":
    unittest.main()
