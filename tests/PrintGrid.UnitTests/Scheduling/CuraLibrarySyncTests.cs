using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

namespace PrintGrid.UnitTests.Scheduling;

public sealed class CuraLibrarySyncTests
{
    [Fact]
    public async Task Missing_configured_python_returns_material_errors_and_keeps_machine_registered()
    {
        using var resources = new Resources();
        await using var db = Database();
        var machine = NewMachine("PLA", "PETG");
        db.Add(machine);
        await db.SaveChangesAsync();
        var configuration = resources.Configuration();
        var service = new CuraLibrarySyncService(db, new CuraResourceCatalog(configuration),
            new MachineProfileRegistry(db, configuration), configuration);

        var result = await service.SyncMachineAsync(machine.Id, CancellationToken.None);

        result.Status.Should().Be("MissingProfile");
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.StartsWith("PLA:"));
        result.Errors.Should().Contain(e => e.StartsWith("PETG:"));
        result.GeneratedMaterials.Should().BeEmpty();
        (await db.Set<Machine>().CountAsync()).Should().Be(1);
        (await db.Set<CuraProfile>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Unsupported_material_explains_why_profile_is_missing()
    {
        using var resources = new Resources();
        await using var db = Database();
        var machine = NewMachine("ABS");
        db.Add(machine);
        await db.SaveChangesAsync();
        var configuration = resources.Configuration();
        var service = new CuraLibrarySyncService(db, new CuraResourceCatalog(configuration),
            new MachineProfileRegistry(db, configuration), configuration);

        var result = await service.SyncMachineAsync(machine.Id, CancellationToken.None);

        result.Status.Should().Be("MissingProfile");
        result.Errors.Should().ContainSingle().Which.Should().Contain("không có vật liệu ABS");
    }

    private static Machine NewMachine(params string[] materials)
    {
        var machine = Machine.Register(Guid.NewGuid(), "Test", "Test printer", PrintTechnology.Fdm,
            BuildVolume.Create(220, 220, 250), .1m, .2m, materials);
        machine.ConfigurePrinter(new PrinterConfiguration("Test", 1, 1, .4m, 1.75m, true,
            260, 100, 6000, .08m, "test", "test_extruder", "", ""));
        return machine;
    }

    private static SyncDb Database() => new(new DbContextOptionsBuilder<PrintGridDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class SyncDb(DbContextOptions<PrintGridDbContext> options) : PrintGridDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new MachineConfiguration());
            builder.ApplyConfiguration(new CuraProfileConfiguration());
        }
    }

    private sealed class Resources : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("printgrid-cura-sync-test-");

        public Resources()
        {
            foreach (var folder in new[] { "definitions", "materials", "quality", "variants" })
                Directory.CreateDirectory(Path.Combine(_directory.FullName, folder));
            File.WriteAllText(Path.Combine(_directory.FullName, "definitions", "test.def.json"), """
                {"name":"Test printer","metadata":{"visible":true,"manufacturer":"Test",
                "machine_extruder_trains":{"0":"test_extruder"}},"overrides":{}}
                """);
            foreach (var material in new[] { "PLA", "PETG" })
                File.WriteAllText(Path.Combine(_directory.FullName, "materials", $"{material}.xml.fdm_material"), $$"""
                    <fdmmaterial xmlns="http://www.ultimaker.com/material"><metadata><name>
                    <brand>Generic</brand><material>{{material}}</material></name></metadata>
                    <properties><diameter>1.75</diameter></properties></fdmmaterial>
                    """);
            File.WriteAllText(Path.Combine(_directory.FullName, "quality", "standard.cfg"), """
                [general]
                name = Standard
                definition = test
                [metadata]
                quality_type = standard
                """);
            File.WriteAllText(Path.Combine(_directory.FullName, "resolver.py"), "");
        }

        public IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["CuraLibrary:ResourcesPath"] = _directory.FullName,
                ["CuraLibrary:ResolverPath"] = Path.Combine(_directory.FullName, "resolver.py"),
                ["CuraLibrary:PythonExecutablePath"] = Path.Combine(_directory.FullName, "missing-python")
            }).Build();

        public void Dispose() => _directory.Delete(true);
    }
}
