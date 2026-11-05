using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Infrastructure.Slicing;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;
using PrintGrid.SharedKernel.Interfaces;

var root = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--")) ?? Directory.GetCurrentDirectory());
var executable = Path.Combine(root, ".local", "cura-5.11.0", "CuraEngine.exe");
var colors = new[] { "BLUE", "WHITE", "SIGNALRED", "BLACK", "GREEN", "YELLOW", "ORANGE", "GREY", "TRANSPARENT" };
var ids = new[] { Guid.Parse("11111111-1111-4111-8111-111111111101"), Guid.Parse("22222222-2222-4222-8222-222222222202") };
var options = new CuraQuoteOptions { ExecutablePath = executable, TimeoutSeconds = 120 };
if (args.Contains("--registry-api-smoke"))
{
    await ProfileApiSmoke.RunAsync(root);
    return;
}
options.Machines[ids[0].ToString()] = new() { SettingsPath = Path.Combine(root, "infra/slicer/profiles/local-standard-pla.resolved.json"),
    Materials = ["PLA"], Colors = colors, PricePerHour = 6000, FailureRate = .08m };
options.Machines[ids[1].ToString()] = new() { SettingsPath = Path.Combine(root, "infra/slicer/profiles/local-fast-pla.resolved.json"),
    Materials = ["PLA"], Colors = colors, PricePerHour = 7500, FailureRate = .05m };
if (args.Contains("--import-current-profiles"))
{
    await LocalSetup.ImportRegisteredProfilesAsync(root, options, ids);
    return;
}
if (args.Contains("--configure") || args.Contains("--seed-local-db"))
{
    await LocalSetup.ConfigureAsync(root, options);
    if (args.Contains("--seed-local-db")) await LocalSetup.SeedAsync(root, ids, colors);
    return;
}
var slicer = new CuraQuoteSlicer(new LocalFiles(Path.Combine(root, "tests/manual/slicer")), Options.Create(options),
    NullLogger<CuraQuoteSlicer>.Instance, new SmokeProfileRegistry(options));
var rates = PricingParameterSet.Active with { PolishingPerUnit = 30000,
    ColorPricePerKg = new Dictionary<string, decimal> { ["PLA:BLUE"] = 300000, ["PLA:WHITE"] = 290000, ["PLA:SIGNALRED"] = 310000 } };
var reports = new List<object>();
QuoteSliceResult? baseline = null;
foreach (var id in ids)
foreach (var multi in new[] { false, true })
foreach (var size in new[] { "ORIGINAL", "M" })
{
    var name = multi ? "three-color-30x20x20mm.3mf" : "cube-20mm.stl";
    var selected = multi ? new[] { "BLUE", "WHITE", "SIGNALRED" } : new[] { "BLUE" };
    await using var file = File.OpenRead(Path.Combine(root, "tests/manual/slicer", name));
    var geometry = await new PrintSlicingService().AnalyzeAsync(file, multi ? "3MF" : "STL");
    if (!geometry.IsValid || !geometry.IsWatertight) throw new InvalidOperationException($"Fixture invalid: {name}");
    var scale = SlicedQuotePricing.ScaleFor(size, geometry.WidthMm, geometry.DepthMm, geometry.HeightMm);
    var result = await slicer.SliceAsync(new("local/" + name, multi ? "3MF" : "STL", scale,
        "PLA", selected, 1.24m, .2m, 20, 1, id), CancellationToken.None);
    if (id == ids[0] && !multi && size == "ORIGINAL") baseline = result;
    var price = SlicedQuotePricing.Calculate(rates, "PLA", size, geometry.WidthMm, geometry.DepthMm,
        geometry.HeightMm, scale, 1, result, true);
    if (result.PrintMinutes <= 0 || result.Colors.Count != selected.Length || result.Colors.Any(c => c.Grams <= 0))
        throw new InvalidOperationException("CuraEngine did not return all requested colors.");
    Console.WriteLine($"PASS {id}: {name} {size}; {result.PrintMinutes:0.00} min; {result.Colors.Sum(c => c.Grams):0.000} g; {price.TotalAmount:0} VND");
    reports.Add(new { MachineId = id, File = name, Size = size, Result = result, Price = price });
}
var dense = await slicer.SliceAsync(new("local/cube-20mm.stl", "STL", 1, "PLA", ["BLUE"], 1.24m, .2m, 80, 1, ids[0]), CancellationToken.None);
var fine = await slicer.SliceAsync(new("local/cube-20mm.stl", "STL", 1, "PLA", ["BLUE"], 1.24m, .1m, 20, 1, ids[0]), CancellationToken.None);
if (dense.Colors.Sum(c => c.Grams) <= baseline!.Colors.Sum(c => c.Grams) || fine.PrintMinutes <= baseline.PrintMinutes)
    throw new InvalidOperationException("Customer infill/layer overrides did not affect real engine output.");
Console.WriteLine($"PASS override infill 80%: {dense.Colors.Sum(c => c.Grams):0.000} g > baseline {baseline.Colors.Sum(c => c.Grams):0.000} g");
Console.WriteLine($"PASS override layer 0.1 mm: {fine.PrintMinutes:0.00} min > baseline {baseline.PrintMinutes:0.00} min");
reports.Add(new { Check = "customer-infill-80-percent", Result = dense });
reports.Add(new { Check = "customer-layer-0.1-mm", Result = fine });
var output = Path.Combine(root, ".local", "slicer-smoke-results.json");
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("Results: " + output);

internal sealed class LocalFiles(string directory) : IFileStorage
{
    public Task<Stream> DownloadAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream>(File.OpenRead(Path.Combine(directory, Path.GetFileName(objectName))));
    public Task<string> UploadAsync(string bucket, string objectName, Stream content, string contentType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<string> GetPresignedUrlAsync(string bucket, string objectName, TimeSpan expiry) => throw new NotSupportedException();
    public Task DeleteAsync(string bucket, string objectName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task EnsureBucketAsync(string bucket, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

// Explicit test-only registry: production resolves approved profiles from PostgreSQL.
internal sealed class SmokeProfileRegistry(CuraQuoteOptions options) : ICuraProfileRegistry
{
    public Task<IReadOnlyList<Guid>> GetApprovedMachineIdsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>(options.Machines.Keys.Select(Guid.Parse).ToArray());
    public async Task<RegisteredCuraSettings?> ResolveAsync(Guid machineId, string material, CancellationToken cancellationToken)
    {
        if (!options.Machines.TryGetValue(machineId.ToString(), out var profile) || !profile.Materials.Contains(material)) return null;
        return new(await File.ReadAllTextAsync(profile.SettingsPath, cancellationToken), 4, profile.PricePerHour!.Value, profile.FailureRate!.Value);
    }
}
