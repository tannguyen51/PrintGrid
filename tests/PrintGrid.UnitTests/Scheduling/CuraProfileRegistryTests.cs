using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Services;
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

    [Fact]
    public void Round_bed_profile_can_be_registered_for_slicing_with_bed_shape_validation()
    {
        var profile = CuraProfileImporter.Parse("Round", "Test Lab", "TEST-220", "PLA", "1", "5.11.0", 1,
            _json.Replace("rectangular", "elliptic"));
        profile.Matches(Machine()).Should().BeTrue();
    }

    [Fact]
    public void Settings_identity_ignores_jsonb_formatting_and_key_order()
    {
        var profile = Profile();
        var original = System.Text.Json.Nodes.JsonNode.Parse(_json)!.AsObject();
        var persisted = new System.Text.Json.Nodes.JsonObject();
        foreach (var pair in original.Reverse()) persisted[pair.Key] = pair.Value!.DeepClone();
        profile.HasSameSettings(persisted.ToJsonString(new System.Text.Json.JsonSerializerOptions
            { WriteIndented = true })).Should().BeTrue();
        persisted["global"]!["speed_print"] = 99;
        profile.HasSameSettings(persisted.ToJsonString()).Should().BeFalse();
    }

    [Theory]
    [InlineData("99.822", 99.82)]
    [InlineData("99.825", 99.83)]
    public void Dimensions_match_at_the_precision_persisted_for_machines(string originalHeight, decimal storedHeight)
    {
        var machine = Machine();
        machine.UpdateSpecifications("M1", "test-220", PrintTechnology.Fdm,
            BuildVolume.Create(220, 220, storedHeight), .1m, .2m, ["PLA"]);
        var profile = CuraProfileImporter.Parse("Round", "Test Lab", "TEST-220", "PLA", "1", "5.11.0", 1,
            _json.Replace("\"machine_height\":250", $"\"machine_height\":{originalHeight}"));
        profile.Matches(machine).Should().BeTrue();
        machine.UpdateSpecifications("M1", "test-220", PrintTechnology.Fdm,
            BuildVolume.Create(220, 220, 100), .1m, .2m, ["PLA"]);
        profile.Matches(machine).Should().BeFalse();
    }

    [Fact]
    public async Task Lab_proposal_workflow_keeps_profile_approval_separate_from_machine_approval()
    {
        await using var db = Database();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var lab = Lab.Onboard("Test Lab", "HCM", 1);
        lab.SetMaterialStock("PLA", "BLACK", 1_000m);

        var machine = Modules.Scheduling.Domain.Entities.Machine.Register(
            lab.Id, "Nova #1", "NP-300", PrintTechnology.Fdm,
            BuildVolume.Create(220, 220, 250), 0.1m, 0.2m, ["PLA"]);
        machine.ConfigurePrinter(new PrinterConfiguration("NovaPrint", 1, 1, 0.4m, 1.75m, true, 260, 100, 6000, 0.05m));
        lab.AddMachine(machine);

        var proposal = CuraProfileImporter.Parse(
            "NP-300 PLA", "NovaPrint", "NP-300", "PLA", "1.0", "5.11.0", 1, _json,
            isActive: false, approvalStatus: CuraProfile.StatusPendingReview,
            submittedByLabId: lab.Id, submittedByUserId: ownerId, targetMachineId: machine.Id,
            submissionNotes: "Initial proposal", sourceFileName: "np300.resolved.json");

        db.AddRange(machine, proposal);
        await db.SaveChangesAsync();

        var registry = Registry(db);
        await registry.MatchAsync(machine, CancellationToken.None);
        machine.ProfileStatus.Should().Be("MissingProfile");
        proposal.Matches(machine).Should().BeFalse();

        // Reject without reason fails
        var emptyReject = () => proposal.RejectProposal(adminId, " ");
        emptyReject.Should().Throw<ArgumentException>();

        // Reject with reason keeps record for lab review
        proposal.RejectProposal(adminId, "Cần kiểm tra lại tốc độ tường");
        proposal.ApprovalStatus.Should().Be(CuraProfile.StatusRejected);
        proposal.IsActive.Should().BeFalse();
        proposal.ReviewReason.Should().Be("Cần kiểm tra lại tốc độ tường");

        // Lab edits and resubmits
        var replacement = CuraProfileImporter.Parse(
            "NP-300 PLA", "NovaPrint", "NP-300", "PLA", "2.0", "5.11.0", 1, _json,
            isActive: false, approvalStatus: CuraProfile.StatusPendingReview,
            submittedByLabId: lab.Id, submittedByUserId: ownerId, targetMachineId: machine.Id,
            submissionNotes: "Updated wall speed", sourceFileName: "np300-v2.resolved.json");
        proposal.ResubmitFrom(replacement, ownerId, machine.Id, "Updated wall speed", "np300-v2.resolved.json");
        proposal.ApprovalStatus.Should().Be(CuraProfile.StatusPendingReview);
        proposal.ReviewReason.Should().BeNull();
        proposal.Version.Should().Be("2.0");

        // Admin approves the profile -> machine becomes PendingReview, NOT Approved
        proposal.ApproveProposal(adminId, "Profile đạt chuẩn");
        await db.SaveChangesAsync();
        await registry.MatchAsync(machine, CancellationToken.None);
        await db.SaveChangesAsync();

        proposal.ApprovalStatus.Should().Be(CuraProfile.StatusApproved);
        proposal.IsActive.Should().BeTrue();
        machine.ProfileStatus.Should().Be("PendingReview");
        machine.CuraProfileIds.Should().Contain(proposal.Id);

        // Machine still cannot quote or be scheduled until machine itself is approved
        (await registry.GetApprovedMachineIdsAsync(CancellationToken.None)).Should().BeEmpty();
        (await registry.ResolveAsync(machine.Id, "PLA", CancellationToken.None)).Should().BeNull();
        var preApprovalCap = CapabilityFilter.Filter(SpecBuilder.Default(), [lab]);
        preApprovalCap.Candidates.Should().BeEmpty();
        preApprovalCap.Rejections.Single().Reason.Should().Be("machine_profile_not_approved");

        // Admin approves the machine -> now eligible for quoting and scheduling
        machine.ReviewProfile(true, adminId, null);
        await db.SaveChangesAsync();

        (await registry.GetApprovedMachineIdsAsync(CancellationToken.None)).Should().Equal(machine.Id);
        (await registry.ResolveAsync(machine.Id, "PLA", CancellationToken.None)).Should().NotBeNull();
        var postApprovalCap = CapabilityFilter.Filter(SpecBuilder.Default(), [lab]);
        postApprovalCap.Candidates.Should().ContainSingle(c => c.Machine.Id == machine.Id);
    }

    [Fact]
    public void Pending_profile_cannot_be_activated_before_approval()
    {
        var proposal = CuraProfileImporter.Parse(
            "Pending", "NovaPrint", "NP-300", "PLA", "1.0", "5.11.0", 1, _json,
            isActive: false, approvalStatus: CuraProfile.StatusPendingReview);

        var act = () => proposal.Activate();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Profile_with_cura_definition_matches_machine_with_empty_definition_and_rejects_different_definition()
    {
        var profile = Profile();
        profile.MarkVerified("creality_k1max", "creality_extruder_0", "0.4mm", "standard");

        var machineWithoutDefId = Machine();
        profile.Matches(machineWithoutDefId).Should().BeTrue();

        var machineWithMatchingDefId = Machine();
        machineWithMatchingDefId.ConfigurePrinter(machineWithMatchingDefId.PrinterConfiguration! with { CuraDefinitionId = "creality_k1max" });
        profile.Matches(machineWithMatchingDefId).Should().BeTrue();

        var machineWithOtherDefId = Machine();
        machineWithOtherDefId.ConfigurePrinter(machineWithOtherDefId.PrinterConfiguration! with { CuraDefinitionId = "prusa_mk4" });
        profile.Matches(machineWithOtherDefId).Should().BeFalse();
    }
}
