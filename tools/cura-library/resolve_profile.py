#!/usr/bin/env python3
"""Resolve one trusted Cura 5.11 resource selection into PrintGrid stack JSON."""
import argparse
import ast
import configparser
import copy
import json
import math
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def safe_id(value):
    if not value or "/" in value or "\\" in value or ".." in value:
        raise ValueError(f"Invalid Cura resource id: {value!r}")
    return value


def flatten(items):
    result = {}
    for key, definition in items.items():
        if definition.get("type") != "category":
            result[key] = copy.deepcopy(definition)
        result.update(flatten(definition.get("children", {})))
    return result


def merge_setting(target, key, value):
    target[key] = {**target.get(key, {}), **copy.deepcopy(value)}


def definition_chain(root, directory, identifier, suffix):
    chain = []
    current = safe_id(identifier)
    seen = set()
    while current:
        if current in seen:
            raise ValueError(f"Cyclic Cura inheritance at {current}")
        seen.add(current)
        path = root / directory / f"{current}{suffix}"
        if not path.exists() and directory == "extruders":
            path = root / "definitions" / f"{current}{suffix}"
        data = json.loads(path.read_text(encoding="utf-8"))
        chain.append((current, data))
        current = data.get("inherits")
    chain.reverse()
    return chain


def definitions(root, machine_id, extruder_id):
    result = {}
    metadata = {}
    name = machine_id
    for _, data in definition_chain(root, "definitions", machine_id, ".def.json"):
        name = data.get("name", name)
        metadata.update(data.get("metadata", {}))
        for key, value in flatten(data.get("settings", {})).items():
            merge_setting(result, key, value)
        for key, value in data.get("overrides", {}).items():
            merge_setting(result, key, value)
    for _, data in definition_chain(root, "extruders", extruder_id, ".def.json"):
        for key, value in flatten(data.get("settings", {})).items():
            merge_setting(result, key, value)
        for key, value in data.get("overrides", {}).items():
            merge_setting(result, key, value)
    return result, metadata, name


def cfg_values(root, directory, identifier):
    if not identifier:
        return {}, {}
    safe_id(identifier)
    matches = list((root / directory).rglob(f"{identifier}.*.cfg")) + list((root / directory).rglob(f"{identifier}.cfg"))
    if not matches:
        raise ValueError(f"Cura {directory} preset not found: {identifier}")
    parser = configparser.ConfigParser(interpolation=None)
    parser.optionxform = str
    parser.read(matches[0], encoding="utf-8")
    return dict(parser.items("values")) if parser.has_section("values") else {}, dict(parser.items("metadata")) if parser.has_section("metadata") else {}


MATERIAL_KEYS = {
    "print temperature": "material_print_temperature",
    "heated bed temperature": "material_bed_temperature",
    "standby temperature": "material_standby_temperature",
    "print cooling": "cool_fan_speed",
}


def material_values(root, identifier):
    safe_id(identifier)
    path = root / "materials" / f"{identifier}.xml.fdm_material"
    if not path.exists():
        matches = list((root / "materials").glob(f"{identifier}*.xml.fdm_material"))
        if not matches:
            raise ValueError(f"Cura material not found: {identifier}")
        path = matches[0]
    tree = ET.parse(path)
    ns = {"m": "http://www.ultimaker.com/material"}
    values = {}
    density = tree.findtext("m:properties/m:density", namespaces=ns)
    diameter = tree.findtext("m:properties/m:diameter", namespaces=ns)
    if density: values["material_density"] = float(density)
    if diameter: values["material_diameter"] = float(diameter)
    for setting in tree.findall("m:settings/m:setting", ns):
        key = MATERIAL_KEYS.get(setting.attrib.get("key", ""))
        if key and setting.text:
            values[key] = parse_literal(setting.text)
    if "material_print_temperature" in values:
        values.setdefault("material_print_temperature_layer_0", values["material_print_temperature"])
        values.setdefault("material_initial_print_temperature", values["material_print_temperature"])
        values.setdefault("material_final_print_temperature", values["material_print_temperature"])
    if "material_bed_temperature" in values:
        values.setdefault("material_bed_temperature_layer_0", values["material_bed_temperature"])
    return values


def parse_literal(value):
    text = value.strip()
    if text.startswith("="):
        return text[1:]
    if text.lower() in ("true", "false"):
        return text.lower() == "true"
    try:
        return float(text) if "." in text else int(text)
    except ValueError:
        return text


class Resolver:
    def __init__(self, definitions_, overrides, count):
        self.defs = definitions_
        self.overrides = overrides
        self.count = count
        self.cache = {}
        self.active = set()

    def value(self, key, index=0):
        if key == "extruder_nr": return index
        if key in self.overrides:
            value = self.overrides[key]
            return self.expression(value, key, index) if isinstance(value, str) else value
        identity = (key, index)
        if identity in self.cache: return self.cache[identity]
        definition = self.defs[key]
        if identity in self.active: return copy.deepcopy(definition.get("default_value", 0))
        self.active.add(identity)
        try:
            raw = definition.get("value", definition.get("default_value"))
            result = self.expression(raw, key, index) if isinstance(raw, str) else copy.deepcopy(raw)
            kind = definition.get("type")
            if kind in ("int", "extruder", "optional_extruder"): result = int(result)
            elif kind == "float":
                result = float(result)
                if not math.isfinite(result): raise ValueError(f"Non-finite setting: {key}")
            self.cache[identity] = result
            return result
        finally:
            self.active.remove(identity)

    def expression(self, expression, key, index):
        try: tree = ast.parse(expression, mode="eval")
        except (SyntaxError, ValueError): return expression
        helpers = {
            "math": math, "max": max, "min": min, "sum": sum, "round": round,
            "int": int, "float": float, "str": str, "bool": bool, "any": any,
            "all": all, "len": len, "map": map, "abs": abs,
            "extruderValue": lambda number, name: self.value(name, max(0, int(number))),
            "extruderValues": lambda name: [self.value(name, i) for i in range(self.count)],
            "resolveOrValue": lambda name: self.value(name, index),
            "defaultExtruderPosition": lambda: 0,
            "anyExtruderWithMaterial": lambda name: next((i for i in range(self.count) if self.value(name, i)), 0),
        }
        locals_ = {n.id for n in ast.walk(tree) if isinstance(n, ast.Name) and isinstance(n.ctx, ast.Store)}
        for name in {n.id for n in ast.walk(tree) if isinstance(n, ast.Name)} - locals_:
            if name in self.defs and name not in helpers: helpers[name] = self.value(name, index)
        try: return eval(compile(tree, f"<cura:{key}>", "eval"), {"__builtins__": {}, **helpers})
        except NameError:
            return expression
        except (TypeError, ValueError, ZeroDivisionError, AttributeError):
            default = self.defs[key].get("default_value")
            if default is not None and default != expression: return default
            raise


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--resources", type=Path, required=True)
    parser.add_argument("--definition", required=True)
    parser.add_argument("--extruder", required=True)
    parser.add_argument("--material", required=True)
    parser.add_argument("--variant", default="")
    parser.add_argument("--quality", required=True)
    parser.add_argument("--slots", type=int, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.slots < 1 or args.slots > 8: raise ValueError("Color slots must be 1..8")
    defs, metadata, machine_name = definitions(args.resources, args.definition, args.extruder)
    variant, _ = cfg_values(args.resources, "variants", args.variant)
    quality, quality_meta = cfg_values(args.resources, "quality", args.quality)
    overrides = {**material_values(args.resources, args.material),
                 **{k: parse_literal(v) for k, v in variant.items()},
                 **{k: parse_literal(v) for k, v in quality.items()},
                 "machine_extruder_count": args.slots}
    resolver = Resolver(defs, overrides, args.slots)
    output = {"global": {key: resolver.value(key) for key in defs}}
    for index in range(args.slots): output[f"extruder.{index}"] = {key: resolver.value(key, index) for key in defs}
    args.output.write_text(json.dumps(output, ensure_ascii=False, allow_nan=False, separators=(",", ":")), encoding="utf-8")
    print(json.dumps({"machineName": machine_name, "manufacturer": metadata.get("manufacturer", "Unknown"),
                      "qualityType": quality_meta.get("quality_type", "standard"), "settings": len(defs)}))


if __name__ == "__main__":
    try: main()
    except Exception as error:
        print(str(error), file=sys.stderr)
        sys.exit(2)
