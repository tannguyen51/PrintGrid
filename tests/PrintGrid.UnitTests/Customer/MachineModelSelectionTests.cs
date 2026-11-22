using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.Modules.Customer.Infrastructure.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

public sealed class MachineModelSelectionTests
{
    private sealed class SelectionDb(DbContextOptions<PrintGridDbContext> options) : PrintGridDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder b)
        {
            b.ApplyConfiguration(new LabConfiguration());
            b.ApplyConfiguration(new MachineConfiguration());
            b.ApplyConfiguration(new CuraProfileConfiguration());
            b.ApplyConfiguration(new MaterialStockConfiguration());
            b.ApplyConfiguration(new MaterialReservationConfiguration());
            b.ApplyConfiguration(new StockTransactionConfiguration());
            b.ApplyConfiguration(new LabStaffConfiguration());
        }
    }

    private static SelectionDb Database() => new(new DbContextOptionsBuilder<PrintGridDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static MachineSelectionCriteria Criteria(TechConstraints? tech = null) =>
        new("PLA", ["BLACK"], 80, 60, 40, .2m, .2m, tech);

    private static Machine AddMachine(Lab lab, bool enclosed = false, int colors = 1, decimal nozzle = .4m)
    {
        var m = Machine.Register(lab.Id, Guid.NewGuid().ToString(), "Model X", PrintTechnology.Fdm,
            BuildVolume.Create(220, 220, 250), .08m, .15m, ["PLA"]);
        m.ConfigurePrinter(new PrinterConfiguration("Maker", 1, colors, nozzle, 1.75m, true, 300, 120, 6000, .08m,
            EnclosedChamber: enclosed));
        m.SetMatchedProfiles([Guid.NewGuid()], true); m.ReviewProfile(true, Guid.NewGuid(), null);
        lab.AddMachine(m); lab.SetMaterialStock("PLA", "BLACK", 10000);
        return m;
    }

    private static async Task<(Lab First, Lab Second)> Seed(SelectionDb db, int firstCount = 2, int secondCount = 1,
        bool enclosed = false, int colors = 1)
    {
        var a = Lab.Onboard("Secret A", "HCM", 1); var b = Lab.Onboard("Secret B", "HCM", 1);
        for (var i = 0; i < firstCount; i++) AddMachine(a, enclosed, colors);
        for (var i = 0; i < secondCount; i++) AddMachine(b, enclosed, colors);
        foreach (var machine in a.Machines.Concat(b.Machines))
        {
            var profile = CuraProfile.Import("Standard", "Maker", "Model X", "PLA", "1", "5.11.0",
                .4m, 1.75m, 1, colors, 220, 220, 250, true, 205, 60, "{}");
            machine.SetMatchedProfiles([profile.Id], true); machine.ReviewProfile(true, Guid.NewGuid(), null);
            db.Add(profile);
        }
        db.AddRange(a, b); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        return (a, b);
    }

    [Theory]
    [InlineData(2, 0, 0)]
    [InlineData(3, 0, 0)]
    [InlineData(2, 1, 1)]
    public async Task Model_requires_three_matching_machines_across_two_labs(int a, int b, int expected)
    {
        await using var db = Database(); await Seed(db, a, b);
        var options = await new EfMachineFilterService(db).GetOptionsAsync(Criteria(), 3, 2, 1.15m, default);
        options.Models.Should().HaveCount(expected);
        if (expected > 0)
        {
            options.Models.Single().Id.Should().Be(MachineModelIdentity.For("maker", "model x"));
            options.Models.Single().ApprovedMachineCount.Should().Be(3);
            var json = System.Text.Json.JsonSerializer.Serialize(options);
            json.Should().NotContain("Secret").And.NotContain("LabId").And.NotContain("LabCount");
        }
    }

    [Fact]
    public async Task Enclosure_is_independent_from_heated_bed_and_no_enclosure_requirement_allows_both()
    {
        await using var db = Database(); await Seed(db);
        var service = new EfMachineFilterService(db);
        var required = await service.GetOptionsAsync(Criteria(TechConstraints.From(null, true, null, null, null)), 3, 2, 1.15m, default);
        required.MatchingMachineCount.Should().Be(0);
        var optional = await service.GetOptionsAsync(Criteria(TechConstraints.From(null, false, null, null, null)), 3, 2, 1.15m, default);
        optional.MatchingMachineCount.Should().Be(3);
    }

    [Fact]
    public async Task Model_options_apply_actual_colors_size_nozzle_tolerance_and_layer_requirements()
    {
        await using var db = Database(); await Seed(db, enclosed: true, colors: 4);
        var service = new EfMachineFilterService(db);
        var valid = Criteria(TechConstraints.From(4, true, .4m, .15m, .1m));
        (await service.GetOptionsAsync(valid, 3, 2, 1.15m, default)).Models.Should().ContainSingle();
        foreach (var impossible in new[] { valid with { WidthMm = 300 }, valid with { ToleranceMm = .01m },
                     valid with { TechConstraints = TechConstraints.From(null, null, .2m, null, null) },
                     valid with { TechConstraints = TechConstraints.From(null, null, null, null, .05m) },
                     valid with { Colors = ["BLACK", "WHITE"] } })
            (await service.GetOptionsAsync(impossible, 3, 2, 1.15m, default)).Models.Should().BeEmpty();
    }

    [Fact]
    public async Task Pinned_model_returns_all_matching_machine_ids_and_rejects_an_individual_machine_id()
    {
        await using var db = Database(); var seed = await Seed(db);
        var ids = seed.First.Machines.Concat(seed.Second.Machines).Select(m => m.Id).ToList();
        var service = new EfMachineFilterService(db);
        (await service.FilterByPinModelAsync(MachineModelIdentity.For("Maker", "Model X"), ids, default)).pinnedIds.Should().BeEquivalentTo(ids);
        (await service.FilterByPinModelAsync(ids[0], ids, default)).pinnedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Admin_disabling_model_is_respected_even_when_disabled_machine_is_offline()
    {
        await using var db = Database(); await Seed(db, 3, 1);
        var machine = await db.Set<Machine>().FirstAsync();
        machine.ToggleCustomerPinning(false); machine.SetStatus(MachineStatus.Offline); await db.SaveChangesAsync();
        var options = await new EfMachineFilterService(db).GetOptionsAsync(Criteria(), 3, 2, 1.15m, default);
        options.MatchingMachineCount.Should().Be(3);
        options.Models.Should().BeEmpty();
    }

    [Fact]
    public async Task A_configured_reference_profile_can_be_used_when_its_source_machine_is_offline()
    {
        await using var db = Database(); var seed = await Seed(db);
        var source = await db.Set<Machine>().SingleAsync(m => m.Id == seed.First.Machines.First().Id);
        source.SetStatus(MachineStatus.Offline); await db.SaveChangesAsync();
        var targets = seed.Second.Machines.Select(m => m.Id).ToList();
        var reference = await new EfMachineFilterService(db).GetReferenceMachineAsync(targets, "PLA", source.CuraProfileIds.Single(), default);
        reference!.MachineId.Should().Be(source.Id);
        reference.ProfileId.Should().Be(source.CuraProfileIds.Single());
    }
}
