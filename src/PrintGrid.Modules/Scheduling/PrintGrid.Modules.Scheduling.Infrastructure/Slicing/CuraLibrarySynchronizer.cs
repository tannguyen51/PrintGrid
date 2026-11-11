using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

public sealed record CuraSyncResult(Guid MachineId, string Status, IReadOnlyList<Guid> ProfileIds,
    IReadOnlyList<string> GeneratedMaterials, IReadOnlyList<string> Errors);

public sealed class CuraLibrarySynchronizer(PrintGridDbContext db, CuraResourceCatalog catalog,
    IMachineProfileMatcher matcher, IConfiguration configuration)
{
    public async Task<CuraSyncResult> SyncMachineAsync(Guid machineId, CancellationToken ct)
    {
        var machine = await db.Set<Machine>().SingleOrDefaultAsync(x => x.Id == machineId, ct)
                      ?? throw new KeyNotFoundException("Không tìm thấy máy.");
        var config = machine.PrinterConfiguration ?? throw new ArgumentException("Máy chưa có cấu hình máy in.");
        if (string.IsNullOrWhiteSpace(config.CuraDefinitionId))
            throw new ArgumentException("Hãy chọn model từ thư viện Cura trước khi đồng bộ profile.");
        var options = catalog.GetOptions(config.CuraDefinitionId);
        if (!options.Machine.ExtruderDefinitionId.Equals(config.CuraExtruderDefinitionId, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Extruder definition không thuộc machine definition đã chọn.");

        var generated = new List<string>();
        var errors = new List<string>();
        foreach (var materialCode in machine.SupportedMaterials)
        {
            try
            {
                var material = ChooseMaterial(options.Materials, materialCode, config.FilamentDiameterMm);
                var quality = ChooseQuality(options.Qualities, material.Id, options.Machine.PreferredQualityType,
                    config.NozzleDiameterMm, config.CuraQualityId);
                var variant = config.CuraVariantId;
                var json = await ResolveAsync(config, material.Id, variant, quality.Id, ct);
                var profile = CuraProfileImporter.Parse(
                    $"{options.Machine.Name} · {materialCode} · {quality.Name}", options.Machine.Manufacturer,
                    options.Machine.Name, materialCode, catalog.Version, catalog.Version, config.ExtruderCount, json);
                profile.MarkVerified(config.CuraDefinitionId, config.CuraExtruderDefinitionId, variant, quality.Id);
                await SmokeTestAsync(json, ct);

                var old = await db.Set<CuraProfile>().Where(x => x.IsActive && x.IsVerified &&
                    x.CuraDefinitionId == config.CuraDefinitionId && x.MaterialCode == materialCode &&
                    x.NozzleDiameterMm == config.NozzleDiameterMm && x.MaximumColorSlots == config.MaximumColorSlots)
                    .ToListAsync(ct);
                foreach (var existing in old) existing.Deactivate();
                db.Set<CuraProfile>().Add(profile);
                await db.SaveChangesAsync(ct);
                generated.Add(materialCode);
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or IOException or JsonException)
            { errors.Add($"{materialCode}: {error.Message}"); }
        }

        await matcher.MatchAsync(machine, ct);
        await db.SaveChangesAsync(ct);
        return new(machine.Id, machine.ProfileStatus, machine.CuraProfileIds, generated, errors);
    }

    private static CuraMaterialDefinition ChooseMaterial(IEnumerable<CuraMaterialDefinition> materials,
        string code, decimal diameter) => materials
        .Where(x => x.MaterialCode.Equals(code, StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => Math.Abs(x.DiameterMm - diameter)).ThenByDescending(x => x.Brand == "Generic")
        .FirstOrDefault() ?? throw new ArgumentException($"Thư viện Cura không có vật liệu {code}.");

    private static CuraPresetDefinition ChooseQuality(IEnumerable<CuraPresetDefinition> qualities, string materialId,
        string preferredType, decimal nozzle, string requested)
    {
        var values = qualities.Where(x => string.IsNullOrWhiteSpace(x.MaterialId) ||
                                           x.MaterialId.Equals(materialId, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(requested))
            return values.FirstOrDefault(x => x.Id.Equals(requested, StringComparison.OrdinalIgnoreCase))
                   ?? throw new ArgumentException("Quality profile đã chọn không tương thích máy/vật liệu.");
        var nozzleText = nozzle.ToString("0.0#", CultureInfo.InvariantCulture);
        return values.OrderByDescending(x => x.QualityType?.Equals(preferredType, StringComparison.OrdinalIgnoreCase) == true)
            .ThenByDescending(x => x.VariantId?.Contains(nozzleText, StringComparison.OrdinalIgnoreCase) == true)
            .FirstOrDefault() ?? throw new ArgumentException("Không có quality profile tương thích.");
    }

    private async Task<string> ResolveAsync(Domain.ValueObjects.PrinterConfiguration printer, string material,
        string variant, string quality, CancellationToken ct)
    {
        var resolver = configuration["CuraLibrary:ResolverPath"];
        var resources = catalog.ResourcesPath;
        if (string.IsNullOrWhiteSpace(resolver)) resolver = FindResolver();
        if (string.IsNullOrWhiteSpace(resolver) || !File.Exists(resolver) || !Directory.Exists(resources))
            throw new InvalidOperationException("Server chưa cấu hình CuraLibrary resolver/resources.");
        var output = Path.GetTempFileName();
        try
        {
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardError = true, RedirectStandardOutput = true };
            foreach (var value in new[] { resolver, "--resources", resources, "--definition", printer.CuraDefinitionId,
                         "--extruder", printer.CuraExtruderDefinitionId, "--material", material, "--quality", quality,
                         "--slots", printer.MaximumColorSlots.ToString(CultureInfo.InvariantCulture), "--output", output })
                start.ArgumentList.Add(value);
            if (!string.IsNullOrWhiteSpace(variant)) { start.ArgumentList.Add("--variant"); start.ArgumentList.Add(variant); }
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Không khởi động được Cura resolver.");
            var stdout = process.StandardOutput.ReadToEndAsync(ct);
            var stderr = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            if (process.ExitCode != 0) throw new InvalidOperationException((await stderr).Trim());
            _ = await stdout;
            return await File.ReadAllTextAsync(output, ct);
        }
        finally { File.Delete(output); }
    }

    private static string FindResolver()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var current = new DirectoryInfo(start); current is not null; current = current.Parent)
            {
                var candidate = Path.Combine(current.FullName, "tools", "cura-library", "resolve_profile.py");
                if (File.Exists(candidate)) return candidate;
            }
        return "";
    }

    private async Task SmokeTestAsync(string settingsJson, CancellationToken ct)
    {
        var executable = configuration["QuoteSlicer:ExecutablePath"];
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            throw new InvalidOperationException("CuraEngine 5.11.0 không tồn tại trên server.");
        var root = JsonNode.Parse(settingsJson)?.AsObject() ?? throw new JsonException("Resolved profile không hợp lệ.");
        var global = root["global"]?.AsObject() ?? throw new JsonException("Resolved profile thiếu global.");
        var extruder = root["extruder.0"]?.AsObject() ?? throw new JsonException("Resolved profile thiếu extruder.0.");
        var directory = Directory.CreateTempSubdirectory("printgrid-cura-verify-");
        try
        {
            var globalPath = Path.Combine(directory.FullName, "global.def.json");
            var extruderPath = Path.Combine(directory.FullName, "extruder.def.json");
            await WriteDefinition(globalPath, global, ct); await WriteDefinition(extruderPath, extruder, ct);
            var model = Path.Combine(directory.FullName, "cube.stl");
            await File.WriteAllTextAsync(model, CubeStl, ct);
            var output = Path.Combine(directory.FullName, "verify.gcode");
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardError = true, RedirectStandardOutput = true };
            foreach (var arg in new[] { "slice", "-j", globalPath, "-o", output, "-e0", "-j", extruderPath,
                         "-l", model, "-s", "center_object=true" }) start.ArgumentList.Add(arg);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Không khởi động được CuraEngine.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(60));
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            var detail = (await stderr) + (await stdout);
            if (process.ExitCode != 0 || !File.Exists(output) || new FileInfo(output).Length == 0)
                throw new InvalidOperationException($"CuraEngine smoke-test thất bại: {detail[^Math.Min(detail.Length, 500)..]}");
        }
        finally { directory.Delete(true); }
    }

    private static Task WriteDefinition(string path, JsonObject values, CancellationToken ct)
    {
        var settings = new JsonObject();
        foreach (var pair in values) settings[pair.Key] = new JsonObject { ["default_value"] = pair.Value?.DeepClone() };
        return File.WriteAllTextAsync(path, new JsonObject { ["settings"] = settings }.ToJsonString(), ct);
    }

    private const string CubeStl = """
        solid verification_tetrahedron
        facet normal 0 0 -1 outer loop vertex 0 0 0 vertex 0 10 0 vertex 10 0 0 endloop endfacet
        facet normal 0 -1 0 outer loop vertex 0 0 0 vertex 10 0 0 vertex 0 0 10 endloop endfacet
        facet normal -1 0 0 outer loop vertex 0 0 0 vertex 0 0 10 vertex 0 10 0 endloop endfacet
        facet normal 1 1 1 outer loop vertex 10 0 0 vertex 0 10 0 vertex 0 0 10 endloop endfacet
        endsolid verification_tetrahedron
        """;
}
