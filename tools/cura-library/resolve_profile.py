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
    machine_chain = definition_chain(root, "definitions", machine_id, ".def.json")
    for _, data in machine_chain:
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
    return result, metadata, name, [item[0] for item in machine_chain]


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


def global_quality_values(root, machine_ids, quality_metadata):
    """Apply Cura's global quality layer(s) before the material/variant quality file."""
    quality_type = quality_metadata.get("quality_type", "")
    layers = []
    for path in (root / "quality").rglob("*.cfg"):
        parser = configparser.ConfigParser(interpolation=None)
        parser.optionxform = str
        try: parser.read(path, encoding="utf-8")
        except (configparser.Error, UnicodeError): continue
        if not parser.has_section("metadata") or not parser.has_section("general"):
            continue
        metadata = dict(parser.items("metadata"))
        if metadata.get("global_quality", "").lower() != "true" or metadata.get("quality_type") != quality_type:
            continue
        definition = parser.get("general", "definition", fallback="")
        if definition not in machine_ids:
            continue
        layers.append((machine_ids.index(definition), dict(parser.items("values")) if parser.has_section("values") else {}))
    result = {}
    for _, values in sorted(layers, key=lambda item: item[0]):
        result.update(values)
    return result


MATERIAL_KEYS = {
    "print temperature": "material_print_temperature",
    "heated bed temperature": "material_bed_temperature",
    "standby temperature": "material_standby_temperature",
    "print cooling": "cool_fan_speed",
    "retraction amount": "retraction_amount",
    "retraction speed": "retraction_speed",
    "build volume temperature": "build_volume_temperature",
}


def material_values(root, identifier, machine_ids, nozzle):
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
    def apply(settings):
        for setting in list(settings):
            if setting.tag not in {"{http://www.ultimaker.com/material}setting",
                                   "{http://www.ultimaker.com/cura}setting"} or not setting.text:
                continue
            raw_key = setting.attrib.get("key", "")
            key = raw_key if setting.tag.startswith("{http://www.ultimaker.com/cura}") else MATERIAL_KEYS.get(raw_key)
            if key: values[key] = parse_literal(setting.text)

    settings = tree.find("m:settings", ns)
    if settings is not None:
        apply(settings)
        for machine in settings.findall("m:machine", ns):
            products = {item.attrib.get("product", "") for item in machine.findall("m:machine_identifier", ns)}
            if not products.intersection(machine_ids):
                continue
            apply(machine)
            nozzle_text = format(nozzle, "g")
            for hotend in machine.findall("m:hotend", ns):
                if nozzle_text in hotend.attrib.get("id", ""):
                    apply(hotend)
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
    defs, metadata, machine_name, machine_ids = definitions(args.resources, args.definition, args.extruder)
    variant, _ = cfg_values(args.resources, "variants", args.variant)
    quality, quality_meta = cfg_values(args.resources, "quality", args.quality)
    global_quality = global_quality_values(args.resources, machine_ids, quality_meta)
    variant_values = {k: parse_literal(v) for k, v in variant.items()}
    nozzle = float(variant_values.get("machine_nozzle_size", 0.4))
    overrides = {**material_values(args.resources, args.material, machine_ids, nozzle),
                 **variant_values,
                 **{k: parse_literal(v) for k, v in global_quality.items()},
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
