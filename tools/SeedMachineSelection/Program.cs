using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using PrintGrid.Api.Bootstrap;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Infrastructure.Services;
using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;
using PrintGrid.SharedKernel.ValueObjects;

// Additive, repeatable fixtures for the local Docker UI. Never uses application/production connection defaults.
var root = Path.GetFullPath(args.FirstOrDefault() ?? Directory.GetCurrentDirectory());
var connection = Environment.GetEnvironmentVariable("PRINTGRID_UI_TEST_DB")
    ?? throw new InvalidOperationException("Set PRINTGRID_UI_TEST_DB to the explicitly selected local database.");
var target = new NpgsqlConnectionStringBuilder(connection);
if (target.Host is not ("localhost" or "127.0.0.1" or "::1") || target.Database != "printgrid_db")
    throw new InvalidOperationException("This seed only accepts loopback host and database printgrid_db.");
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["QuoteSlicer:EngineVersion"] = "5.11.0"
}).Build();
new ServiceCollection().AddApplicationModules(config);
await using var db = new PrintGridDbContext(new DbContextOptionsBuilder<PrintGridDbContext>()
    .UseNpgsql(connection, pg => pg.MigrationsHistoryTable("__ef_migrations_history", "shared")).Options);
await db.Database.MigrateAsync();
await using var transaction = await db.Database.BeginTransactionAsync();

const string demoPassword = "PrintGridUI!2026";
var customer = await Account("machine-ui@printgrid.local", "UI Test Customer", "Customer");
var admin = await Account("machine-ui-admin@printgrid.local", "UI Test Admin", "Admin");
var labA = await LabFixture("[UI TEST] Machine Selection A");
var labB = await LabFixture("[UI TEST] Machine Selection B");
var colors = new[] { "BLACK", "WHITE", "SIGNALRED", "BLUE", "GREEN", "YELLOW", "ORANGE", "GREY", "TRANSPARENT" };
foreach (var lab in new[] { labA, labB })
    foreach (var color in colors)
        if (!lab.MaterialStocks.Any(s => s.MaterialCode == "PLA" && s.ColorCode == color))
            lab.SetMaterialStock("PLA", color, 500_000m, 1000m);

var standard = await Profile("DEMO STANDARD 220", "local-standard-pla.resolved.json");
var fast = await Profile("DEMO FAST 320", "local-fast-pla.resolved.json");
var singleLab = await Profile("DEMO SINGLE LAB 220", "local-standard-pla.resolved.json");
await MachineFixture(labA, standard, "Standard A1", enclosed: false, .1m, .2m);
await MachineFixture(labA, standard, "Standard A2", enclosed: false, .1m, .2m);
await MachineFixture(labB, standard, "Standard B1", enclosed: false, .1m, .2m);
await MachineFixture(labA, fast, "Fast A1", enclosed: true, .05m, .1m);
await MachineFixture(labB, fast, "Fast B1", enclosed: true, .05m, .1m);
await MachineFixture(labB, fast, "Fast B2", enclosed: true, .05m, .1m);
await MachineFixture(labA, singleLab, "Single lab A1", enclosed: false, .1m, .2m);
await db.SaveChangesAsync();

var files = new MinioFileStorage(Options.Create(new MinioOptions
{
    Endpoint = "127.0.0.1:9000", PublicEndpoint = "localhost:9000",
    AccessKey = Environment.GetEnvironmentVariable("PRINTGRID_UI_TEST_MINIO_ACCESS")
        ?? throw new InvalidOperationException("Missing local MinIO access key."),
    SecretKey = Environment.GetEnvironmentVariable("PRINTGRID_UI_TEST_MINIO_SECRET")
        ?? throw new InvalidOperationException("Missing local MinIO secret key.")
}));
var samples = new List<object>();
foreach (var (fileName, name) in new[] {
    ("cube-20mm.stl", "[UI TEST] Khối 20 mm — một màu"),
    ("three-color-30x20x20mm.3mf", "[UI TEST] Khối ba màu BLUE WHITE SIGNALRED") })
{
    var bytes = await File.ReadAllBytesAsync(Path.Combine(root, "tests/manual/slicer", fileName));
    var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    var model = await db.Set<Model>().FirstOrDefaultAsync(m => m.CustomerId == customer.Id && m.Name == name && m.Sha256 == hash);
    var objectName = $"{customer.Id}/machine-selection-ui-test/{hash}{Path.GetExtension(fileName)}";
    await using (var upload = new MemoryStream(bytes))
        await files.UploadAsync("printgrid-models", objectName, upload, "application/octet-stream");
    if (model is null)
    {
        var slicer = new PrintSlicingService();
        await using var content = new MemoryStream(bytes);
        var format = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant();
        var geometry = await slicer.AnalyzeAsync(content, format);
        if (!geometry.IsValid || !geometry.IsWatertight || !geometry.IsManifold)
            throw new InvalidOperationException($"Invalid sample geometry: {fileName}");
        var estimate = slicer.Estimate(geometry, "PLA", .2m, 30, PrintTechnology.Fdm);
        model = Model.Create(customer.Id, name, "Dữ liệu ảo dành riêng cho kiểm thử chọn model máy trên UI local.",
            fileName, format, bytes.Length, ["machine-selection-ui-test"], $"printgrid-models/{objectName}", hash, emitUploadReceipt: false);
        model.ApplyGeometry(geometry.WidthMm, geometry.DepthMm, geometry.HeightMm, geometry.VolumeCm3,
            estimate.PrintMinutes, estimate.MaterialGrams, geometry.IsWatertight, geometry.IsManifold, true);
        model.Approve(admin.Id, DateTime.UtcNow, "Approved deterministic local UI fixture.");
        db.Add(model);
    }
    samples.Add(new { model.Id, model.Name, File = fileName, Url = $"http://localhost:3000/models/{model.Id}/order" });
}
await db.SaveChangesAsync();
await transaction.CommitAsync();

// Verify eligibility with the production filter, rather than merely counting seeded rows.
var filter = new EfMachineFilterService(db, config);
var ordinary = await filter.GetOptionsAsync(new MachineSelectionCriteria("PLA", ["BLACK"], 20, 20, 20, .2m, .2m), 3, 2, 1.15m, default);
foreach (var expected in new[] { standard, fast })
    if (!ordinary.Models.Any(m => m.Id == MachineModelIdentity.For(expected.Manufacturer, expected.MachineModel)))
        throw new InvalidOperationException($"Seeded model is not eligible: {expected.MachineModel}");
if (ordinary.Models.Any(m => m.Id == MachineModelIdentity.For(singleLab.Manufacturer, singleLab.MachineModel)))
    throw new InvalidOperationException("Single-lab control model must remain hidden.");
var manifest = new { Database = target.Database, CustomerEmail = customer.Email, AdminEmail = admin.Email,
    Models = ordinary.Models.Where(m => m.Name.Contains("PRINTGRID UI TEST")).ToList(), Samples = samples };
Directory.CreateDirectory(Path.Combine(root, ".local"));
await File.WriteAllTextAsync(Path.Combine(root, ".local", "machine-selection-ui-seed.json"),
    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(manifest));

async Task<Customer> Account(string email, string name, string role)
{
    var existing = await db.Set<Customer>().SingleOrDefaultAsync(c => c.Email == email);
    if (existing is not null) return existing; // Never reset an existing password or role.
    var created = Customer.Register(email, BCrypt.Net.BCrypt.HashPassword(demoPassword), name, null);
    created.AssignRole(role); created.VerifyEmail(); db.Add(created); return created;
}

async Task<Lab> LabFixture(string name)
{
    var existing = await db.Set<Lab>().Include(l => l.Machines).Include(l => l.MaterialStocks).SingleOrDefaultAsync(l => l.Name == name);
    if (existing is not null) return existing;
    var created = Lab.Onboard(name, "Hồ Chí Minh", 1); db.Add(created); return created;
}

async Task<CuraProfile> Profile(string model, string file)
{
    var existing = await db.Set<CuraProfile>().SingleOrDefaultAsync(p => p.Manufacturer == "PRINTGRID UI TEST"
        && p.MachineModel == model && p.Version == "ui-test-v1");
    if (existing is not null) return existing;
    var json = await File.ReadAllTextAsync(Path.Combine(root, "infra/slicer/profiles", file));
    var created = CuraProfileImporter.Parse($"UI TEST {model}", "PRINTGRID UI TEST", model, "PLA", "ui-test-v1", "5.11.0", 4, json);
    db.Add(created); return created;
}

Task MachineFixture(Lab lab, CuraProfile profile, string name, bool enclosed, decimal layer, decimal tolerance)
{
    if (lab.Machines.Any(m => m.Name == $"[UI TEST] {name}")) return Task.CompletedTask;
    var created = Machine.Register(lab.Id, $"[UI TEST] {name}", profile.MachineModel, PrintTechnology.Fdm,
        BuildVolume.Create(profile.WidthMm, profile.DepthMm, profile.HeightMm), layer, tolerance, ["PLA"]);
    created.ConfigurePrinter(new PrinterConfiguration(profile.Manufacturer, profile.ExtruderCount, profile.MaximumColorSlots,
        profile.NozzleDiameterMm, profile.FilamentDiameterMm, profile.HeatedBed, 260, 100, 6000, .08m, EnclosedChamber: enclosed));
    created.SetMatchedProfiles([profile.Id], true); created.ReviewProfile(true, admin.Id, "Local UI test fixture.");
    lab.AddMachine(created); return Task.CompletedTask;
}
