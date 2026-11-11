using System.Collections.Concurrent;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

public sealed record CuraMachineDefinition(string Id, string Manufacturer, string Name,
    string ExtruderDefinitionId, string PreferredMaterialId, string PreferredQualityType,
    decimal WidthMm, decimal DepthMm, decimal HeightMm, bool HeatedBed, decimal FilamentDiameterMm);
public sealed record CuraMaterialDefinition(string Id, string Brand, string Name, string MaterialCode,
    string Guid, decimal DiameterMm, decimal Density);
public sealed record CuraPresetDefinition(string Id, string Name, string DefinitionId, string? MaterialId,
    string? VariantId, string? QualityType);
public sealed record CuraMachineOptions(CuraMachineDefinition Machine, IReadOnlyList<CuraMaterialDefinition> Materials,
    IReadOnlyList<CuraPresetDefinition> Variants, IReadOnlyList<CuraPresetDefinition> Qualities);

/// <summary>Read-only view over the pinned Cura resource tree. IDs are file stems, never display names.</summary>
public sealed class CuraResourceCatalog
{
    private readonly string _root;
    private readonly Lazy<IReadOnlyList<CuraMachineDefinition>> _machines;
    private readonly Lazy<IReadOnlyList<CuraMaterialDefinition>> _materials;

    public string Version { get; }
    public string ResourcesPath => _root;
    public bool IsAvailable => Directory.Exists(Path.Combine(_root, "definitions"));

    public CuraResourceCatalog(IConfiguration configuration)
    {
        Version = configuration["CuraLibrary:Version"] ?? "5.11.0";
        _root = configuration["CuraLibrary:ResourcesPath"] ?? "";
        if (string.IsNullOrWhiteSpace(_root))
            _root = FindDevelopmentResources(Version);
        _machines = new(LoadMachines, LazyThreadSafetyMode.ExecutionAndPublication);
        _materials = new(LoadMaterials, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private static string FindDevelopmentResources(string version)
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var current = new DirectoryInfo(start); current is not null; current = current.Parent)
            {
                var candidate = Path.Combine(current.FullName, "infra", "slicer", "cura-resources", version);
                if (Directory.Exists(candidate)) return candidate;
            }
        return Path.Combine(AppContext.BaseDirectory, "cura-resources", version);
    }

    public IReadOnlyList<CuraMachineDefinition> Machines => IsAvailable ? _machines.Value : [];
    public IReadOnlyList<CuraMaterialDefinition> Materials => IsAvailable ? _materials.Value : [];

    public CuraMachineOptions GetOptions(string definitionId)
    {
        var machine = Machines.SingleOrDefault(x => x.Id.Equals(definitionId, StringComparison.OrdinalIgnoreCase))
                      ?? throw new KeyNotFoundException("Không tìm thấy machine definition trong thư viện Cura đã ghim.");
        return new(machine, Materials, LoadPresets("variants", definitionId), LoadPresets("quality", definitionId));
    }

    private IReadOnlyList<CuraMachineDefinition> LoadMachines()
    {
        var cache = new ConcurrentDictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        JsonElement Resolve(string id)
        {
            return cache.GetOrAdd(id, key =>
            {
                var path = SafeFile("definitions", key, ".def.json");
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var current = doc.RootElement.Clone();
                if (!current.TryGetProperty("inherits", out var parent)) return current;
                return Merge(Resolve(parent.GetString()!), current);
            });
        }

        var result = new List<CuraMachineDefinition>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(_root, "definitions"), "*.def.json"))
        {
            var id = Path.GetFileName(file)[..^".def.json".Length];
            JsonElement raw;
            try { raw = Resolve(id); } catch { continue; }
            var metadata = raw.TryGetProperty("metadata", out var m) ? m : default;
            if (metadata.ValueKind != JsonValueKind.Object || !Bool(metadata, "visible", false)) continue;
            var extruder = metadata.TryGetProperty("machine_extruder_trains", out var trains) &&
                           trains.TryGetProperty("0", out var train) ? train.GetString() ?? "" : "";
            if (extruder.Length == 0) continue;
            var overrides = raw.TryGetProperty("overrides", out var o) ? o : default;
            result.Add(new(id, Text(metadata, "manufacturer", "Unknown"), Text(raw, "name", id), extruder,
                Text(metadata, "preferred_material", "generic_pla"), Text(metadata, "preferred_quality_type", "standard"),
                DecimalSetting(overrides, "machine_width"), DecimalSetting(overrides, "machine_depth"),
                DecimalSetting(overrides, "machine_height"), BoolSetting(overrides, "machine_heated_bed"),
                DecimalSetting(overrides, "material_diameter", 1.75m)));
        }
        return result.OrderBy(x => x.Manufacturer).ThenBy(x => x.Name).ToList();
    }

    private IReadOnlyList<CuraMaterialDefinition> LoadMaterials()
    {
        XNamespace ns = "http://www.ultimaker.com/material";
        var items = new Dictionary<string, CuraMaterialDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(_root, "materials"), "*.xml.fdm_material"))
        {
            try
            {
                var document = XDocument.Load(file);
                var root = document.Root!;
                var metadata = root.Element(ns + "metadata")!;
                var name = metadata.Element(ns + "name")!;
                var material = name.Element(ns + "material")?.Value ?? Path.GetFileNameWithoutExtension(file);
                var id = Path.GetFileName(file)[..^".xml.fdm_material".Length];
                var properties = root.Element(ns + "properties");
                var item = new CuraMaterialDefinition(id, name.Element(ns + "brand")?.Value ?? "Unknown", material,
                    NormalizeMaterial(material), metadata.Element(ns + "GUID")?.Value ?? "",
                    Parse(properties?.Element(ns + "diameter")?.Value, 1.75m), Parse(properties?.Element(ns + "density")?.Value, 1.24m));
                items.TryAdd(id, item);
            }
            catch { /* A malformed third-party material must not hide the rest of the catalog. */ }
        }
        return items.Values.OrderBy(x => x.Brand).ThenBy(x => x.Name).ToList();
    }

    private IReadOnlyList<CuraPresetDefinition> LoadPresets(string directory, string definitionId)
    {
        var files = Directory.EnumerateFiles(Path.Combine(_root, directory), "*.cfg", SearchOption.AllDirectories);
        var result = new List<CuraPresetDefinition>();
        foreach (var file in files)
        {
            var sections = ParseIni(file);
            if (!sections.TryGetValue("general", out var general) || !sections.TryGetValue("metadata", out var metadata)) continue;
            var definition = general.GetValueOrDefault("definition", "");
            if (definition.Length > 0 && !DefinitionApplies(definitionId, definition)) continue;
            result.Add(new(Path.GetFileName(file)[..^Path.GetExtension(file).Length], general.GetValueOrDefault("name", Path.GetFileName(file)),
                definition, metadata.GetValueOrDefault("material"), metadata.GetValueOrDefault("variant"), metadata.GetValueOrDefault("quality_type")));
        }
        return result.GroupBy(x => x.Id).Select(x => x.First()).OrderBy(x => x.Name).ToList();
    }

    private bool DefinitionApplies(string child, string expected)
    {
        for (var current = child; current.Length > 0;)
        {
            if (current.Equals(expected, StringComparison.OrdinalIgnoreCase)) return true;
            using var doc = JsonDocument.Parse(File.ReadAllText(SafeFile("definitions", current, ".def.json")));
            current = doc.RootElement.TryGetProperty("inherits", out var parent) ? parent.GetString() ?? "" : "";
        }
        return false;
    }

    private string SafeFile(string directory, string id, string suffix)
    {
        if (id.IndexOfAny(['/', '\\']) >= 0 || id.Contains("..", StringComparison.Ordinal)) throw new InvalidDataException("Cura resource ID không hợp lệ.");
        return Path.Combine(_root, directory, id + suffix);
    }

    private static Dictionary<string, Dictionary<string, string>> ParseIni(string path)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string>? current = null;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.StartsWith('[') && line.EndsWith(']')) current = result[line[1..^1]] = new(StringComparer.OrdinalIgnoreCase);
            else if (current is not null && line.Length > 0 && !line.StartsWith('#') && line.Contains('='))
            { var split = line.IndexOf('='); current[line[..split].Trim()] = line[(split + 1)..].Trim(); }
        }
        return result;
    }

    private static JsonElement Merge(JsonElement parent, JsonElement child)
    {
        var map = JsonSerializer.Deserialize<Dictionary<string, object?>>(parent.GetRawText())!;
        foreach (var (key, value) in JsonSerializer.Deserialize<Dictionary<string, object?>>(child.GetRawText())!) map[key] = value;
        if (parent.TryGetProperty("metadata", out var pm) && child.TryGetProperty("metadata", out var cm)) map["metadata"] = Merge(pm, cm);
        if (parent.TryGetProperty("overrides", out var po) && child.TryGetProperty("overrides", out var co)) map["overrides"] = Merge(po, co);
        return JsonSerializer.SerializeToElement(map);
    }
    private static string Text(JsonElement value, string key, string fallback) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(key, out var item) ? item.GetString() ?? fallback : fallback;
    private static bool Bool(JsonElement value, string key, bool fallback) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(key, out var item) && item.ValueKind is JsonValueKind.True or JsonValueKind.False ? item.GetBoolean() : fallback;
    private static decimal DecimalSetting(JsonElement settings, string key, decimal fallback = 0) => settings.ValueKind == JsonValueKind.Object && settings.TryGetProperty(key, out var item) ? SettingDecimal(item, fallback) : fallback;
    private static bool BoolSetting(JsonElement settings, string key) => settings.ValueKind == JsonValueKind.Object && settings.TryGetProperty(key, out var item) && item.TryGetProperty("default_value", out var value) && value.ValueKind == JsonValueKind.True;
    private static decimal SettingDecimal(JsonElement item, decimal fallback) => item.TryGetProperty("default_value", out var d) && d.TryGetDecimal(out var value) ? value : item.TryGetProperty("value", out var v) && v.TryGetDecimal(out value) ? value : fallback;
    private static decimal Parse(string? value, decimal fallback) => decimal.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : fallback;
    private static string NormalizeMaterial(string value) => value.Trim().ToUpperInvariant() switch { "TOUGH PLA" => "PLA", var text => text.Replace(" ", "") };
}
