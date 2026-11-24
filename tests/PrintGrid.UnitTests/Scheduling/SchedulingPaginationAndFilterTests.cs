using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;
using PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Queries;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.UnitTests.Scheduling;

public class SchedulingPaginationAndFilterTests
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

    private sealed class TestDb(DbContextOptions<PrintGridDbContext> options) : PrintGridDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LabConfiguration());
            modelBuilder.ApplyConfiguration(new LabStaffConfiguration());
            modelBuilder.ApplyConfiguration(new MachineConfiguration());
            modelBuilder.ApplyConfiguration(new MaterialStockConfiguration());
            modelBuilder.ApplyConfiguration(new StockTransactionConfiguration());
            modelBuilder.ApplyConfiguration(new MaterialReservationConfiguration());
            modelBuilder.ApplyConfiguration(new CuraProfileConfiguration());
        }
    }

    private static TestDb CreateDb() => new(new DbContextOptionsBuilder<PrintGridDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void PagedResponse_calculates_total_pages_and_clamps_page_size()
    {
        var empty = PagedResponse<string>.Create([], 0, 3, 10);
        empty.TotalCount.Should().Be(0);
        empty.TotalPages.Should().Be(0);
        empty.Page.Should().Be(1);

        var clamped = PagedResponse<string>.Create(["a"], 11, 5, 500);
        clamped.PageSize.Should().Be(100);
        clamped.TotalPages.Should().Be(1);
        clamped.Page.Should().Be(1);

        var multi = PagedResponse<string>.Create(["a", "b", "c", "d", "e"], 12, 2, 5);
        multi.TotalPages.Should().Be(3);
        multi.Page.Should().Be(2);
    }

    [Fact]
    public async Task GetLabsPaged_filters_by_status_active_hardware_material_and_paginates()
    {
        await using var db = CreateDb();

        var owner1 = Guid.NewGuid();
        var labActive = Lab.Onboard("Alpha Lab", "Hà Nội", 1);
        var m1 = Machine.Register(labActive.Id, "A1", "X1 Carbon", PrintTechnology.Fdm,
            BuildVolume.Create(256, 256, 256), 0.08m, 0.1m, ["PLA", "PETG"]);
        m1.ConfigurePrinter(new PrinterConfiguration("Bambu Lab", 1, 1, 0.4m, 1.75m, true, 300, 110, 8000, 0.05m));
        labActive.AddMachine(m1);

        var labPending = Lab.SelfRegister("Beta Lab", "TP.HCM", 2, "08:00-18:00", owner1);
        var m2 = Machine.Register(labPending.Id, "B1", "MK4", PrintTechnology.Fdm,
            BuildVolume.Create(250, 210, 220), 0.1m, 0.15m, ["ABS"]);
        m2.ConfigurePrinter(new PrinterConfiguration("Prusa", 1, 1, 0.4m, 1.75m, true, 290, 110, 7000, 0.06m));
        labPending.AddMachine(m2);

        var labRejected = Lab.SelfRegister("Gamma Lab", "Đà Nẵng", 2, "08:00-17:00", Guid.NewGuid());
        labRejected.Reject("Thiếu thông tin máy");

        var labInactive = Lab.Onboard("Delta Lab", "Cần Thơ", 2);
        labInactive.Deactivate();

        db.AddRange(labActive, labPending, labRejected, labInactive);
        await db.SaveChangesAsync();

        var service = new SchedulingRegistryQueryService(db);

        var allPage1 = await service.GetLabsPagedAsync(new LabPagedQueryFilter(Page: 1, PageSize: 2));
        allPage1.TotalCount.Should().Be(4);
        allPage1.TotalPages.Should().Be(2);
        allPage1.Items.Should().HaveCount(2);
        allPage1.Items[0].Status.Should().Be("Pending");

        var byMaterial = await service.GetLabsPagedAsync(new LabPagedQueryFilter(Material: "pla", Page: 1, PageSize: 10));
        byMaterial.TotalCount.Should().Be(1);
        byMaterial.Items.Single().Name.Should().Be("Alpha Lab");

        var byHw = await service.GetLabsPagedAsync(new LabPagedQueryFilter(ManufacturerOrModel: "prusa", Page: 1, PageSize: 10));
        byHw.TotalCount.Should().Be(1);
        byHw.Items.Single().Name.Should().Be("Beta Lab");

        var byActive = await service.GetLabsPagedAsync(new LabPagedQueryFilter(IsActive: true, Page: 1, PageSize: 10));
        byActive.TotalCount.Should().Be(1);
        byActive.Items.Single().Name.Should().Be("Alpha Lab");

        var byStatus = await service.GetLabsPagedAsync(new LabPagedQueryFilter(ApprovalStatus: "Rejected", Page: 1, PageSize: 10));
        byStatus.TotalCount.Should().Be(1);
        byStatus.Items.Single().Name.Should().Be("Gamma Lab");
    }

    [Fact]
    public async Task GetMachinesPaged_and_GetCuraProfilesPaged_filter_and_compute_eligibility_accurately()
    {
        await using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var labActive = Lab.Onboard("Active Hub Lab", "HCM", 1);
        labActive.SetMaterialStock("PLA", "BLACK", 500m);

        var approvedProfile = CuraProfileImporter.Parse(
            "X1 PLA", "Bambu Lab", "X1C", "PLA", "1.0", "5.11.0", 1, _json,
            isActive: true, approvalStatus: CuraProfile.StatusApproved);
        var pendingProposal = CuraProfileImporter.Parse(
            "MK4 PETG", "Prusa", "MK4", "PETG", "1.0", "5.11.0", 1, _json,
            isActive: false, approvalStatus: CuraProfile.StatusPendingReview,
            submittedByLabId: labActive.Id, submittedByUserId: ownerId,
            submissionNotes: "Need PETG", sourceFileName: "mk4-petg.resolved.json");
        var rejectedProposal = CuraProfileImporter.Parse(
            "Voron ABS", "Voron", "V2.4", "ABS", "1.0", "5.11.0", 1, _json,
            isActive: false, approvalStatus: CuraProfile.StatusPendingReview,
            submittedByLabId: labActive.Id, submittedByUserId: ownerId);
        rejectedProposal.RejectProposal(adminId, "Chưa đủ nhiệt bàn");

        var readyMachine = Machine.Register(labActive.Id, "X1C #01", "X1C", PrintTechnology.Fdm,
            BuildVolume.Create(220, 220, 250), 0.1m, 0.2m, ["PLA"]);
        readyMachine.ConfigurePrinter(new PrinterConfiguration("Bambu Lab", 1, 1, 0.4m, 1.75m, true, 300, 110, 9000, 0.05m));
        readyMachine.SetMatchedProfiles([approvedProfile.Id], true);
        readyMachine.ReviewProfile(true, adminId, null);

        var waitingMachine = Machine.Register(labActive.Id, "X1C #02", "X1C", PrintTechnology.Fdm,
            BuildVolume.Create(220, 220, 250), 0.1m, 0.2m, ["PLA"]);
        waitingMachine.ConfigurePrinter(new PrinterConfiguration("Bambu Lab", 1, 1, 0.4m, 1.75m, true, 300, 110, 9000, 0.05m));
        waitingMachine.SetMatchedProfiles([approvedProfile.Id], true);

        var maintMachine = Machine.Register(labActive.Id, "X1C #03", "X1C", PrintTechnology.Fdm,
            BuildVolume.Create(220, 220, 250), 0.1m, 0.2m, ["PLA", "ABS"]);
        maintMachine.ConfigurePrinter(new PrinterConfiguration("Bambu Lab", 1, 1, 0.4m, 1.75m, true, 300, 110, 9000, 0.05m));
        maintMachine.SetMatchedProfiles([approvedProfile.Id], true);
        maintMachine.ReviewProfile(true, adminId, null);
        maintMachine.SetStatus(MachineStatus.Maintenance);

        labActive.AddMachine(readyMachine);
        labActive.AddMachine(waitingMachine);
        labActive.AddMachine(maintMachine);

        db.AddRange(labActive, approvedProfile, pendingProposal, rejectedProposal);
        await db.SaveChangesAsync();

        var service = new SchedulingRegistryQueryService(db);

        // Machine paging & eligibility
        var machinesPage = await service.GetMachinesPagedAsync(new MachinePagedQueryFilter(Page: 1, PageSize: 2));
        machinesPage.TotalCount.Should().Be(3);
        machinesPage.TotalPages.Should().Be(2);
        machinesPage.Items.Should().HaveCount(2);

        var absMachines = await service.GetMachinesPagedAsync(new MachinePagedQueryFilter(Material: "ABS", Page: 1, PageSize: 10));
        absMachines.TotalCount.Should().Be(1);
        absMachines.Items.Single().Name.Should().Be("X1C #03");
        absMachines.Items.Single().IsEligibleForQuotingAndScheduling.Should().BeFalse();

        var approvedIdle = await service.GetMachinesPagedAsync(
            new MachinePagedQueryFilter(ProfileStatus: "Approved", OperationalStatus: "Idle", Page: 1, PageSize: 10));
        approvedIdle.TotalCount.Should().Be(1);
        approvedIdle.Items.Single().Name.Should().Be("X1C #01");
        approvedIdle.Items.Single().IsEligibleForQuotingAndScheduling.Should().BeTrue();
        approvedIdle.Items.Single().MatchedProfiles.Should().ContainSingle(p => p.Id == approvedProfile.Id);

        // Library vs Proposals queries
        var libraryPage = await service.GetCuraProfilesPagedAsync(
            new CuraProfilePagedQueryFilter(Scope: "library", IsActive: true, Page: 1, PageSize: 10));
        libraryPage.TotalCount.Should().Be(1);
        libraryPage.Items.Single().Name.Should().Be("X1 PLA");

        var proposalsPage = await service.GetCuraProfilesPagedAsync(
            new CuraProfilePagedQueryFilter(Scope: "proposals", LabId: labActive.Id, Page: 1, PageSize: 1));
        proposalsPage.TotalCount.Should().Be(2);
        proposalsPage.TotalPages.Should().Be(2);
        proposalsPage.Items.Single().ApprovalStatus.Should().Be(CuraProfile.StatusPendingReview);

        var rejectedFilter = await service.GetCuraProfilesPagedAsync(
            new CuraProfilePagedQueryFilter(Scope: "proposals", ApprovalStatus: CuraProfile.StatusRejected, Page: 1, PageSize: 10));
        rejectedFilter.TotalCount.Should().Be(1);
        rejectedFilter.Items.Single().ReviewReason.Should().Be("Chưa đủ nhiệt bàn");

        // Deactivating the approved profile immediately marks the machine ineligible in paged queries
        approvedProfile.Deactivate();
        await db.SaveChangesAsync();
        var afterDeactivate = await service.GetMachinesPagedAsync(
            new MachinePagedQueryFilter(ProfileStatus: "Approved", OperationalStatus: "Idle", Page: 1, PageSize: 10));
        afterDeactivate.Items.Should().ContainSingle(x => x.Name == "X1C #01");
        afterDeactivate.Items.Single().IsEligibleForQuotingAndScheduling.Should().BeFalse();
    }
}
