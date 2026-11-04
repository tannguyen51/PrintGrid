"""Generate complete, resolved LOCAL TEST profiles from the pinned Cura definitions.

Only trusted definitions from the official Cura installation are evaluated. This is
not an evaluator for files uploaded by customers. No brand/hardware certification
is implied: these are virtual four-tool FDM rigs for quotation testing.
"""
import argparse
import ast
import copy
import json
import math
import struct
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path


def flatten(items):
    result = {}
    for key, definition in items.items():
        if definition.get("type") != "category":
            result[key] = definition
        result.update(flatten(definition.get("children", {})))
    return result


class Resolver:
    def __init__(self, definitions, overrides, extruder_count=4):
        self.defs = definitions
        self.overrides = overrides
        self.count = extruder_count
        self.cache = {}
        self.active = set()
        self.cycle_defaults = set()

    def value(self, key, index=0):
        if key == "extruder_nr":
            return index
        if key in self.overrides:
            return self.overrides[key]
        identity = (key, index)
        if identity in self.cache:
            return self.cache[identity]
        definition = self.defs[key]
        if identity in self.active:
            self.cycle_defaults.add(key)
            return copy.deepcopy(definition["default_value"])
        self.active.add(identity)
        try:
            expression = definition.get("value")
            if expression is None:
                result = copy.deepcopy(definition["default_value"])
            elif not isinstance(expression, str):
                result = expression
            else:
                tree = ast.parse(expression, mode="eval")
                helpers = {
                    "math": math, "max": max, "min": min, "sum": sum, "round": round,
                    "int": int, "float": float, "str": str, "bool": bool,
                    "any": any, "all": all, "len": len, "map": map, "abs": abs,
                    "extruderValue": lambda number, name: self.value(name, max(0, int(number))),
                    "extruderValues": lambda name: [self.value(name, i) for i in range(self.count)],
                    "resolveOrValue": lambda name: self.value(name, index),
                    "defaultExtruderPosition": lambda: 0,
                    "anyExtruderWithMaterial": lambda name: next(
                        (i for i in range(self.count) if self.value(name, i)), 0),
                }
                # Comprehension variables are local to the expression, not setting names.
                locals_ = {n.id for n in ast.walk(tree) if isinstance(n, ast.Name) and isinstance(n.ctx, ast.Store)}
                names = {n.id for n in ast.walk(tree) if isinstance(n, ast.Name)} - locals_
                for name in names:
                    if name in self.defs and name not in helpers:
                        helpers[name] = self.value(name, index)
                result = eval(compile(tree, "<trusted-cura-definition>", "eval"), {"__builtins__": {}, **helpers})
            kind = definition.get("type")
            if kind in ("int", "extruder", "optional_extruder"):
                result = int(result)
            elif kind == "float":
                result = float(result)
                if not math.isfinite(result):
                    raise ValueError(f"Non-finite setting: {key}")
            self.cache[identity] = result
            return result
        finally:
            self.active.remove(identity)


def cube_triangles(origin, size):
    x, y, z = origin
    a, b, c = size
    v = [(x,y,z),(x+a,y,z),(x+a,y+b,z),(x,y+b,z),
         (x,y,z+c),(x+a,y,z+c),(x+a,y+b,z+c),(x,y+b,z+c)]
    faces = [(0,2,1),(0,3,2),(4,5,6),(4,6,7),(0,1,5),(0,5,4),
             (1,2,6),(1,6,5),(2,3,7),(2,7,6),(3,0,4),(3,4,7)]
    return v, faces


def fixtures(output):
    output.mkdir(parents=True, exist_ok=True)
    vertices, faces = cube_triangles((0,0,0), (20,20,20))
    with (output / "cube-20mm.stl").open("wb") as file:
        file.write(b"PrintGrid local quotation smoke test".ljust(80, b"\0"))
        file.write(struct.pack("<I", len(faces)))
        for face in faces:
            coords = [n for vertex in face for n in vertices[vertex]]
            file.write(struct.pack("<12fH", 0, 0, 0, *coords, 0))
    ns = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02"
    ET.register_namespace("", ns)
    def tag(name): return f"{{{ns}}}{name}"
    root = ET.Element(tag("model"), unit="millimeter")
    resources = ET.SubElement(root, tag("resources"))
    materials = ET.SubElement(resources, tag("basematerials"), id="1")
    for name, color in [("BLUE", "#0000FFFF"),("WHITE", "#FFFFFFFF"),("SIGNALRED", "#FF0000FF")]:
        ET.SubElement(materials, tag("base"), name=name, displaycolor=color)
    build = ET.SubElement(root, tag("build"))
    for i in range(3):
        obj = ET.SubElement(resources, tag("object"), id=str(i+2), type="model", pid="1", pindex=str(i))
        mesh = ET.SubElement(obj, tag("mesh"))
        vv = ET.SubElement(mesh, tag("vertices"))
        vertices, faces = cube_triangles((i*10,0,0), (10,20,20))
        for x,y,z in vertices: ET.SubElement(vv, tag("vertex"), x=str(x), y=str(y), z=str(z))
        tt = ET.SubElement(mesh, tag("triangles"))
        for a,b,c in faces: ET.SubElement(tt, tag("triangle"), v1=str(a), v2=str(b), v3=str(c))
        ET.SubElement(build, tag("item"), objectid=str(i+2))
    with zipfile.ZipFile(output / "three-color-30x20x20mm.3mf", "w", zipfile.ZIP_DEFLATED) as z:
        z.writestr("3D/3dmodel.model", ET.tostring(root, encoding="utf-8", xml_declaration=True))
        z.writestr("[Content_Types].xml", '<?xml version="1.0"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="model" ContentType="application/vnd.ms-package.3dmanufacturing-3dmodel+xml"/></Types>')
        z.writestr("_rels/.rels", '<?xml version="1.0"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Target="/3D/3dmodel.model" Id="rel0" Type="http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel"/></Relationships>')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--cura", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--fixtures", type=Path, required=True)
    args = parser.parse_args()
    base = args.cura / "share/cura/resources/definitions"
    printer = flatten(json.loads((base / "fdmprinter.def.json").read_text(encoding="utf-8"))["settings"])
    extruder = flatten(json.loads((base / "fdmextruder.def.json").read_text(encoding="utf-8"))["settings"])
    definitions = {**printer}
    for key, values in extruder.items():
        definitions[key] = {**definitions.get(key, {}), **values}
    common = {
        "machine_extruder_count": 4, "machine_gcode_flavor": "RepRap (Marlin/Sprinter)",
        "machine_center_is_zero": False, "machine_heated_bed": True,
        "machine_nozzle_size": .4, "material_diameter": 1.75,
        "material_print_temperature": 205, "material_print_temperature_layer_0": 210,
        "material_initial_print_temperature": 205, "material_final_print_temperature": 195,
        "material_bed_temperature": 60, "material_bed_temperature_layer_0": 60,
        "material_density": 1.24, "material_flow": 100,
        "layer_height": .2, "layer_height_0": .2, "line_width": .4,
        "wall_line_count": 3, "wall_thickness": 1.2,
        "top_layers": 5, "bottom_layers": 5, "top_bottom_thickness": 1,
        "infill_sparse_density": 20, "infill_pattern": "grid",
        "support_enable": True, "support_type": "everywhere", "support_angle": 55,
        "support_pattern": "zigzag", "support_infill_rate": 15,
        "support_z_distance": .2, "support_xy_distance": .7,
        "adhesion_type": "brim", "brim_width": 4,
        "retraction_enable": True, "retraction_amount": 1.2, "retraction_speed": 35,
        "retraction_hop_enabled": False,
        "cool_fan_enabled": True, "cool_fan_speed": 100, "cool_fan_speed_0": 0,
        "cool_min_layer_time": 5, "cool_min_speed": 10,
        "acceleration_enabled": True, "jerk_enabled": True,
        "prime_tower_enable": True, "prime_tower_size": 18, "prime_tower_min_volume": 15,
        "prime_tower_position_x": 195, "prime_tower_position_y": 195,
        "prime_tower_brim_enable": True, "ooze_shield_enabled": False,
        "machine_extruder_start_code": "", "machine_extruder_end_code": "",
        "machine_start_gcode": "G28\nG92 E0", "machine_end_gcode": "M104 S0\nM140 S0\nM107",
        "machine_nozzle_offset_x": 0, "machine_nozzle_offset_y": 0,
        "machine_extruders_share_heater": False, "machine_extruders_share_nozzle": False,
        "machine_use_extruder_offset_to_offset_coords": True,
        "machine_nozzle_heat_up_speed": 2, "machine_nozzle_cool_down_speed": 2,
        "machine_minimum_feedrate": 0, "machine_max_feedrate_z": 10,
        "machine_max_feedrate_e": 45,
    }
    profiles = [
        ("local-standard-pla", {"machine_name": "LOCAL TEST FDM 220 - four virtual tools",
            "machine_width":220, "machine_depth":220, "machine_height":250,
            "speed_print":50, "speed_travel":120, "speed_wall_0":25,
            "speed_layer_0":20, "acceleration_print":500, "acceleration_travel":1000,
            "jerk_print":8, "machine_max_feedrate_x":150, "machine_max_feedrate_y":150,
            "machine_max_acceleration_x":1000, "machine_max_acceleration_y":1000,
            "machine_acceleration":500}),
        ("local-fast-pla", {"machine_name": "LOCAL TEST FDM 320 - four virtual tools",
            "machine_width":320, "machine_depth":320, "machine_height":350,
            "speed_print":90, "speed_travel":180, "speed_wall_0":40,
            "speed_layer_0":25, "acceleration_print":1500, "acceleration_travel":2500,
            "jerk_print":10, "machine_max_feedrate_x":250, "machine_max_feedrate_y":250,
            "machine_max_acceleration_x":3000, "machine_max_acceleration_y":3000,
            "machine_acceleration":1500, "prime_tower_position_x":290, "prime_tower_position_y":290}),
    ]
    args.output.mkdir(parents=True, exist_ok=True)
    for name, overrides in profiles:
        resolver = Resolver(definitions, {**common, **overrides})
        resolved = {"global": {k: resolver.value(k) for k in definitions}}
        for i in range(4): resolved[f"extruder.{i}"] = {k: resolver.value(k, i) for k in definitions}
        path = args.output / f"{name}.resolved.json"
        path.write_text(json.dumps(resolved, indent=2, ensure_ascii=False, allow_nan=False)+"\n", encoding="utf-8")
        print(f"{path}: {len(definitions)} settings/stack; cycle defaults={sorted(resolver.cycle_defaults)}")
    fixtures(args.fixtures)


if __name__ == "__main__":
    main()
