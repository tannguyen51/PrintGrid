using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

namespace PrintGrid.UnitTests.Scheduling;

public class CuraProfileRegistryTests
{
    private const string _json = """
    {"global":{"machine_extruder_count":1,"machine_width":220,"machine_depth":220,"machine_height":250,
    "machine_shape":"rectangular","machine_heated_bed":true,"machine_center_is_zero":false,
    "machine_max_feedrate_x":150,"machine_max_feedrate_y":150,"machine_acceleration":500,"speed_print":50,
    "layer_height":0.2,"machine_nozzle_size":0.4,"material_diameter":1.75},
    "extruder.0":{"machine_nozzle_size":0.4,"material_diameter":1.75,"speed_print":50,"speed_wall":25,
    "speed_travel":100,"layer_height":0.2,"material_print_temperature":205,"material_print_temperature_layer_0":210,
    "material_bed_temperature":60,"material_bed_temperature_layer_0":60}}
    """;

    private static CuraProfile Profile(string material = "PLA", string model = "TEST-220", string engine = "5.11.0") =>
        CuraProfileImporter.Parse("Standard", "Test Lab", model, material, "1", engine, 1, _json);

    private static Machine Machine(params string[] materials)
    {
        var machine = DomainMachine();
        machine.UpdateSpecifications("M1", "test-220", PrintTechnology.Fdm, BuildVolume.Create(220, 220, 250), .1m, .2m,
            materials.Length == 0 ? ["PLA"] : materials);
        machine.ConfigurePrinter(new PrinterConfiguration("test lab", 1, 1, .4m, 1.75m, true, 260, 100, 6000, .08m));
        return machine;
    }

    private static Machine DomainMachine() => Modules.Scheduling.Domain.Entities.Machine.Register(Guid.NewGuid(), "M1", "test-220",
        PrintTechnology.Fdm, BuildVolume.Create(220, 220, 250), .1m, .2m, ["PLA"]);

    private sealed class RegistryDb(DbContextOptions<PrintGridDbContext> options) : PrintGridDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new MachineConfiguration());
            builder.ApplyConfiguration(new CuraProfileConfiguration());
        }
    }

    private static RegistryDb Database() => new(new DbContextOptionsBuilder<PrintGridDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static MachineProfileRegistry Registry(RegistryDb db) => new(db, new ConfigurationBuilder().Build());

    [Fact]
    public async Task Matching_profile_is_attached_but_cannot_quote_until_approved()
    {
        await using var db = Database(); var machine = Machine(); var profile = Profile();
        db.AddRange(machine, profile); await db.SaveChangesAsync();
        var registry = Registry(db);
        await registry.MatchAsync(machine, CancellationToken.None); await db.SaveChangesAsync();
        machine.ProfileStatus.Should().Be("PendingReview");
        machine.CuraProfileIds.Should().Equal(profile.Id);
        (await registry.GetApprovedMachineIdsAsync(CancellationToken.None)).Should().BeEmpty();
        (await registry.ResolveAsync(machine.Id, "PLA", CancellationToken.None)).Should().BeNull();
        machine.ReviewProfile(true, Guid.NewGuid(), null); await db.SaveChangesAsync();
        (await registry.GetApprovedMachineIdsAsync(CancellationToken.None)).Should().Equal(machine.Id);
        var resolved = await registry.ResolveAsync(machine.Id, "PLA", CancellationToken.None);
        resolved!.PricePerHour.Should().Be(6000); resolved.FailureRate.Should().Be(.08m);
        resolved.SettingsJson.Should().Be(profile.SettingsJson);
    }

    [Fact]
    public async Task Missing_material_or_wrong_hardware_keeps_machine_out_of_quotes()
    {
        await using var db = Database(); var machine = Machine("PLA", "PETG");
        db.AddRange(machine, Profile(), Profile("PETG", "OTHER-MODEL")); await db.SaveChangesAsync();
        await Registry(db).MatchAsync(machine, CancellationToken.None);
        machine.ProfileStatus.Should().Be("MissingProfile"); machine.CuraProfileIds.Should().HaveCount(1);
        var approve = () => machine.ReviewProfile(true, Guid.NewGuid(), null);
        approve.Should().Throw<ArgumentException>();
        db.Add(Profile("PETG")); await db.SaveChangesAsync();
        await Registry(db).MatchAsync(machine, CancellationToken.None);
        machine.ProfileStatus.Should().Be("PendingReview"); machine.CuraProfileIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task Wrong_engine_version_is_never_matched()
    {
        await using var db = Database(); var machine = Machine();
        db.AddRange(machine, Profile(engine: "4.0.0")); await db.SaveChangesAsync();
        await Registry(db).MatchAsync(machine, CancellationToken.None);
        machine.ProfileStatus.Should().Be("MissingProfile");
    }

    [Fact]
    public async Task Rejection_and_specification_changes_revoke_eligibility()
    {
        await using var db = Database(); var machine = Machine(); db.AddRange(machine, Profile()); await db.SaveChangesAsync();
        var registry = Registry(db); await registry.MatchAsync(machine, CancellationToken.None);
        machine.ReviewProfile(true, Guid.NewGuid(), null); await db.SaveChangesAsync();
        machine.ReviewProfile(false, Guid.NewGuid(), "Sai đầu phun"); await db.SaveChangesAsync();
        (await registry.ResolveAsync(machine.Id, "PLA", CancellationToken.None)).Should().BeNull();
        machine.ConfigurePrinter(machine.PrinterConfiguration! with { NozzleDiameterMm = .6m });
        await registry.MatchAsync(machine, CancellationToken.None); await db.SaveChangesAsync();
        machine.ProfileStatus.Should().Be("MissingProfile"); machine.CuraProfileIds.Should().BeEmpty();
    }

    [Fact]
    public void Temperature_or_size_mismatch_is_not_compatible()
    {
        var machine = Machine(); var profile = Profile();
        profile.Matches(machine).Should().BeTrue();
        machine.ConfigurePrinter(machine.PrinterConfiguration! with { MaxNozzleTemperatureC = 200 });
        profile.Matches(machine).Should().BeFalse();
        machine.ConfigurePrinter(machine.PrinterConfiguration! with { MaxNozzleTemperatureC = 260 });
        machine.UpdateSpecifications("M1", "test-220", PrintTechnology.Fdm, BuildVolume.Create(300, 300, 300), .1m, .2m, ["PLA"]);
        profile.Matches(machine).Should().BeFalse();
    }

    [Fact]
    public void Missing_profile_blocks_capability_even_when_operational_status_is_idle()
    {
        var machine = Machine();
        machine.SetStatus(MachineStatus.Idle);
        machine.Supports(SpecBuilder.Default()).Should().BeFalse();
    }

    [Theory]
    [InlineData("local-standard-pla.resolved.json")]
    [InlineData("local-fast-pla.resolved.json")]
    public void Bundled_resolved_profiles_pass_import_validation(string file)
    {
        var profile = CuraProfileImporter.Parse("Local test", "PrintGrid Test", "Virtual", "PLA", "1", "5.11.0", 4,
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "profiles", file)));
        profile.MaximumColorSlots.Should().Be(4);
    }

    [Fact]
    public void Unresolved_cura_definition_is_rejected()
    {
        var import = () => CuraProfileImporter.Parse("x", "x", "x", "PLA", "1", "5.11.0", 1, "{\"inherits\":\"fdmprinter\"}");
        import.Should().Throw<ArgumentException>();
    }
}
