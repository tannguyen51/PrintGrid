using NSubstitute;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Scheduling;

public class Week4LabFeaturesTests
{
    private readonly ILabRepository _labs = Substitute.For<ILabRepository>();
    private readonly IJobRepository _jobs = Substitute.For<IJobRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IModelRepository _models = Substitute.For<IModelRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task FR_LAB_001_AC01_Self_registered_pending_lab_does_not_enter_capability_filter_until_approved()
    {
        var selfRegister = new SelfRegisterLabCommandHandler(_labs, _unitOfWork);
        var ownerId = Guid.NewGuid();

        Lab? capturedLab = null;
        await _labs.AddAsync(Arg.Do<Lab>(l => capturedLab = l), Arg.Any<CancellationToken>());

        var regResult = await selfRegister.Handle(
            new SelfRegisterLabCommand("Xưởng In Tân Bình", "TP.HCM", 1, "T2-T7 08:00-18:00", ownerId, "0909123456", "12 Cộng Hòa"),
            CancellationToken.None);

        regResult.IsSuccess.Should().BeTrue();
        regResult.Value.Status.Should().Be("Pending");
        regResult.Value.IsActive.Should().BeFalse();
        capturedLab.Should().NotBeNull();

        // Even if the pending lab adds a capable machine, CapabilityFilter must NOT select it before Admin approval (AC01)
        var machine = Machine.Register(
            capturedLab!.Id,
            "Bambu X1C #1",
            "X1 Carbon",
            PrintTechnology.Fdm,
            BuildVolume.Create(256, 256, 256),
            0.08m,
            0.15m,
            new[] { "PLA", "PETG" });
        capturedLab.AddMachine(machine);

        var filter = new CapabilityFilter();
        var spec = JobSpecification.Create(
            BuildVolume.Create(100, 100, 100),
            "PLA",
            "BLACK",
            0.2m,
            0.2m,
            PrintTechnology.Fdm,
            50m);

        var beforeApproval = filter.Filter(spec, new[] { capturedLab });
        beforeApproval.Candidates.Should().BeEmpty("pending labs must not receive jobs before admin approval (AC01)");

        // Admin approves the lab -> PENDING -> ACTIVE
        _labs.GetByIdAsync(capturedLab.Id, Arg.Any<CancellationToken>()).Returns(capturedLab);
        var approveHandler = new ApproveLabCommandHandler(_labs, _unitOfWork);
        var approveResult = await approveHandler.Handle(new ApproveLabCommand(capturedLab.Id, Guid.NewGuid()), CancellationToken.None);

        approveResult.IsSuccess.Should().BeTrue();
        approveResult.Value.Status.Should().Be("Active");
        approveResult.Value.IsActive.Should().BeTrue();

        var afterApproval = filter.Filter(spec, new[] { capturedLab });
        afterApproval.Candidates.Should().ContainSingle("approved lab with complete profile and machine enters filter");
    }

    [Fact]
    public async Task FR_LAB_001_AC02_Admin_cannot_approve_lab_when_it_has_zero_machines()
    {
        var ownerId = Guid.NewGuid();
        var pendingLab = Lab.SelfRegister("Xưởng Chưa Khai Máy", "Đà Nẵng", 2, "T2-T6 08:00-17:30", ownerId);
        _labs.GetByIdAsync(pendingLab.Id, Arg.Any<CancellationToken>()).Returns(pendingLab);

        var approveHandler = new ApproveLabCommandHandler(_labs, _unitOfWork);
        var result = await approveHandler.Handle(new ApproveLabCommand(pendingLab.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue("a lab without machines cannot be approved (AC02 / BR-OPS-006)");
        result.Error.Code.Should().Be("conflict");
        pendingLab.Status.Should().Be(LabStatus.Pending);
        pendingLab.IsActive.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FR_LAB_002_AC01_and_AC02_Machine_CRUD_own_lab_scoping_offline_filter_and_block_delete_with_active_jobs()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        // Ops-created lab (active), then claimed by the owner: this test is about machine CRUD
        // and the capability filter, not the approval workflow.
        var lab = Lab.Onboard("Xưởng Cầu Giấy", "Hà Nội", 1, operatingHours: "T2-T7 08:00-18:00");
        lab.ClaimOwnership(ownerId);
        var machine = Machine.Register(
            lab.Id,
            "Prusa MK4 #1",
            "MK4",
            PrintTechnology.Fdm,
            BuildVolume.Create(250, 210, 220),
            0.05m,
            0.15m,
            new[] { "PLA" });
        lab.AddMachine(machine);
        _labs.GetByIdAsync(lab.Id, Arg.Any<CancellationToken>()).Returns(lab);

        var crudHandler = new MachineCrudCommandHandler(_labs, _jobs, _unitOfWork);

        // 1. Foreign LabManager cannot edit or delete another lab's machine ("chỉ máy mình")
        var foreignAttempt = await crudHandler.Handle(
            new DeleteMachineCommand(lab.Id, machine.Id, RequestingUserId: otherUserId, IsAdminOrOps: false),
            CancellationToken.None);
        foreignAttempt.IsFailure.Should().BeTrue();
        foreignAttempt.Error.Code.Should().Be("forbidden");

        // 2. AC01: Offline or scheduled maintenance immediately removes machine from CapabilityFilter
        var spec = JobSpecification.Create(
            BuildVolume.Create(100, 100, 100),
            "PLA",
            "BLACK",
            0.2m,
            0.2m,
            PrintTechnology.Fdm,
            40m);
        var filter = new CapabilityFilter();
        filter.Filter(spec, new[] { lab }).Candidates.Should().ContainSingle();

        machine.SetStatus(MachineStatus.Offline);
        var offlineFiltered = filter.Filter(spec, new[] { lab });
        offlineFiltered.Candidates.Should().BeEmpty("offline machine is filtered out immediately (AC01)");
        offlineFiltered.Rejections.Single().Reason.Should().Be("machine_offline");

        // Restore Idle then schedule an active maintenance window
        machine.SetStatus(MachineStatus.Idle);
        await crudHandler.Handle(
            new ScheduleMachineMaintenanceCommand(
                lab.Id,
                machine.Id,
                DateTime.UtcNow.AddMinutes(-10),
                DateTime.UtcNow.AddHours(2),
                "Bảo trì cụm đầu phun",
                Clear: false,
                SetMaintenanceStatusNow: true,
                RequestingUserId: ownerId,
                IsAdminOrOps: false),
            CancellationToken.None);
        filter.Filter(spec, new[] { lab }).Candidates.Should().BeEmpty("machine in maintenance window is filtered out");

        // 3. AC02: Deleting a machine that still has an active job is blocked
        var activeJob = Job.Create(Guid.NewGuid(), Guid.NewGuid(), spec, 120, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));
        activeJob.AssignTo(lab.Id, machine.Id, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 0.90m, DateTime.UtcNow);
        _jobs.GetActiveByMachineAsync(machine.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Job> { activeJob });

        var blockedDelete = await crudHandler.Handle(
            new DeleteMachineCommand(lab.Id, machine.Id, RequestingUserId: ownerId, IsAdminOrOps: false),
            CancellationToken.None);
        blockedDelete.IsFailure.Should().BeTrue("deleting a machine with active jobs must be blocked (AC02)");
        blockedDelete.Error.Code.Should().Be("conflict");
        lab.Machines.Should().ContainSingle();

        // Once active jobs are cleared, owner can delete the machine
        _jobs.GetActiveByMachineAsync(machine.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Job>());
        var allowedDelete = await crudHandler.Handle(
            new DeleteMachineCommand(lab.Id, machine.Id, RequestingUserId: ownerId, IsAdminOrOps: false),
            CancellationToken.None);
        allowedDelete.IsSuccess.Should().BeTrue();
        lab.Machines.Should().BeEmpty();
    }

    [Fact]
    public async Task FR_LAB_006_AC01_Completing_a_job_removes_its_slot_from_future_Gantt_timeline()
    {
        var lab = Lab.Onboard("Xưởng Bách Khoa", "Hà Nội", 1);
        var machine = Machine.Register(
            lab.Id,
            "Voron 2.4 #1",
            "Voron 2.4",
            PrintTechnology.Fdm,
            BuildVolume.Create(300, 300, 300),
            0.05m,
            0.1m,
            new[] { "PLA", "ABS" });
        lab.AddMachine(machine);
        _labs.GetByIdAsync(lab.Id, Arg.Any<CancellationToken>()).Returns(lab);

        var spec = JobSpecification.Create(
            BuildVolume.Create(80, 80, 50),
            "PLA",
            "BLACK",
            0.2m,
            0.2m,
            PrintTechnology.Fdm,
            35m);

        var now = DateTime.UtcNow;
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), spec, 180, DateOnly.FromDateTime(now.AddDays(2)));
        job.AssignTo(lab.Id, machine.Id, now.AddHours(1), now.AddHours(4), 0.90m, now);
        job.Accept();
        job.Start(now);

        _jobs.GetByLabIdAsync(lab.Id, Arg.Any<CancellationToken>()).Returns(new List<Job> { job });

        var queryHandler = new GetLabTimelineAndPerformanceQueryHandler(_labs, _jobs, _orders, _models);

        var beforeComplete = await queryHandler.Handle(new GetLabTimelineQuery(lab.Id), CancellationToken.None);
        beforeComplete.IsSuccess.Should().BeTrue();
        beforeComplete.Value.Machines.Single().Slots.Should().ContainSingle(s => s.JobId == job.Id);

        // Complete the job -> AC01: slot must disappear from future Gantt timeline
        job.Complete(now, 170, "In đạt chuẩn", new[] { "proof-1.jpg" }, 35m);

        var afterComplete = await queryHandler.Handle(new GetLabTimelineQuery(lab.Id), CancellationToken.None);
        afterComplete.IsSuccess.Should().BeTrue();
        afterComplete.Value.Machines.Single().Slots.Should().BeEmpty(
            "completed job slot must disappear from the future timeline (AC01)");
    }

    [Fact]
    public async Task FR_LAB_007_AC02_Performance_score_returns_4_metrics_provisional_score_under_10_jobs_and_anonymous_benchmark()
    {
        var lab = Lab.Onboard("Xưởng Thủ Đức", "TP.HCM", 1);
        var peerLab = Lab.Onboard("Xưởng Đối Tác Ẩn Danh", "Hà Nội", 2);
        var machine = Machine.Register(
            lab.Id,
            "Prusa #1",
            "MK4",
            PrintTechnology.Fdm,
            BuildVolume.Create(250, 210, 210),
            0.05m,
            0.15m,
            new[] { "PLA" });
        lab.AddMachine(machine);

        _labs.GetByIdAsync(lab.Id, Arg.Any<CancellationToken>()).Returns(lab);
        _labs.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Lab> { lab, peerLab });

        var spec = JobSpecification.Create(
            BuildVolume.Create(50, 50, 50),
            "PLA",
            "WHITE",
            0.2m,
            0.2m,
            PrintTechnology.Fdm,
            20m);

        var now = DateTime.UtcNow;
        var fiveJobs = Enumerable.Range(0, 5).Select(_ =>
        {
            var j = Job.Create(Guid.NewGuid(), Guid.NewGuid(), spec, 60, DateOnly.FromDateTime(now.AddDays(2)));
            j.AssignTo(lab.Id, machine.Id, now.AddMinutes(-90), now.AddMinutes(-30), 0.90m, now.AddMinutes(-100));
            j.Accept(now.AddMinutes(-95));
            j.Start(now.AddMinutes(-90));
            j.Complete(now.AddMinutes(-30), 58, "OK", new[] { "ok.jpg" }, 20m);
            return j;
        }).ToList();

        _jobs.GetAllAssignedToLabsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(fiveJobs);

        var handler = new GetLabTimelineAndPerformanceQueryHandler(_labs, _jobs, _orders, _models);

        // < 10 completed jobs => provisional score 0.70 (điểm tạm)
        var under10 = await handler.Handle(new GetLabPerformanceQuery(lab.Id), CancellationToken.None);
        under10.IsSuccess.Should().BeTrue();
        under10.Value.CompletedJobsCount.Should().Be(5);
        under10.Value.IsProvisional.Should().BeTrue();
        under10.Value.StandingScore.Should().Be(0.70m);
        under10.Value.LabMetrics.OnTimeDeliveryRate.Should().Be(1.0m);
        under10.Value.LabMetrics.FirstPassYield.Should().Be(1.0m);
        under10.Value.LabMetrics.AcceptanceRate.Should().Be(1.0m);
        under10.Value.NetworkAnonymousAverage.Should().NotBeNull();
        under10.Value.ActiveLabsInBenchmark.Should().Be(2);

        // >= 10 completed jobs => full composite score calculated from the 4 metrics
        var twelveJobs = Enumerable.Range(0, 12).Select(_ =>
        {
            var j = Job.Create(Guid.NewGuid(), Guid.NewGuid(), spec, 60, DateOnly.FromDateTime(now.AddDays(2)));
            j.AssignTo(lab.Id, machine.Id, now.AddMinutes(-90), now.AddMinutes(-30), 0.90m, now.AddMinutes(-100));
            j.Accept(now.AddMinutes(-95));
            j.Start(now.AddMinutes(-90));
            j.Complete(now.AddMinutes(-30), 58, "OK", new[] { "ok.jpg" }, 20m);
            return j;
        }).ToList();

        _jobs.GetAllAssignedToLabsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(twelveJobs);

        var over10 = await handler.Handle(new GetLabPerformanceQuery(lab.Id), CancellationToken.None);
        over10.IsSuccess.Should().BeTrue();
        over10.Value.CompletedJobsCount.Should().Be(12);
        over10.Value.IsProvisional.Should().BeFalse();
        over10.Value.StandingScore.Should().BeGreaterThan(0.85m);
    }
}
