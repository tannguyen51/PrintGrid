import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
RESOURCES = ROOT / "infra" / "slicer" / "cura-resources" / "5.11.0"


class ResolveProfileTests(unittest.TestCase):
    def resolve(self, definition, extruder, material):
        with tempfile.TemporaryDirectory(prefix="printgrid-resolver-test-") as directory:
            output = Path(directory) / "resolved.json"
            subprocess.run([
                sys.executable, str(Path(__file__).with_name("resolve_profile.py")),
                "--resources", str(RESOURCES), "--definition", definition,
                "--extruder", extruder, "--material", material,
                "--quality", "normal", "--slots", "1", "--output", str(output),
            ], check=True, capture_output=True, text=True)
            return json.loads(output.read_text(encoding="utf-8"))

    def test_unheated_bed_ignores_material_bed_temperatures(self):
        for material in ("generic_pla_175", "generic_petg_175"):
            with self.subTest(material=material):
                profile = self.resolve("abax_titan", "abax_titan_extruder_0", material)
                for stack in profile.values():
                    self.assertFalse(stack["machine_heated_bed"])
                    self.assertEqual(stack["material_bed_temperature"], 0)
                    self.assertEqual(stack["material_bed_temperature_layer_0"], 0)
                    self.assertGreater(stack["material_print_temperature"], 0)

    def test_round_bed_keeps_shape_and_original_dimensions(self):
        profile = self.resolve("101Hero", "101Hero_extruder_0", "generic_pla_175")
        self.assertEqual(profile["global"]["machine_shape"], "elliptic")
        self.assertEqual(profile["global"]["machine_height"], 99.822)
        self.assertEqual(profile["global"]["machine_width"], 149.86)

    def test_heated_bed_keeps_material_bed_temperatures(self):
        profile = self.resolve("3di_d300", "3di_base_extruder_0", "generic_pla_175")
        self.assertTrue(profile["global"]["machine_heated_bed"])
        self.assertGreater(profile["extruder.0"]["material_bed_temperature"], 0)


if __name__ == "__main__":
    unittest.main()
