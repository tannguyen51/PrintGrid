using NSubstitute;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Scheduling;

/// <summary>FR-ANAL-002/003 / BR-PERF-001 SLA aggregation over the 90-day window.</summary>
public class SlaReportTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    private static Job CompletedJob(
        Guid labId,
        Guid machineId,
        DateOnly internalDueDate,
        DateTime completedAtUtc,
        int estimatedMinutes = 60,
        int actualMinutes = 60)
    {
        var job = Job.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SpecBuilder.Default(),
            estimatedMinutes,
            internalDueDate);

        job.AssignTo(labId, machineId, completedAtUtc.AddHours(-2), completedAtUtc, 0.9m, completedAtUtc.AddHours(-2));
        job.Accept(completedAtUtc.AddHours(-1)).IsSuccess.Should().BeTrue();
        job.Start(completedAtUtc.AddMinutes(-30)).IsSuccess.Should().BeTrue();
        job.Complete(completedAtUtc, actualMinutes, "Đã kiểm tra", ["qc/photo.jpg"]).IsSuccess.Should().BeTrue();
        job.ReviewQcProof(true, Guid.NewGuid(), null, completedAtUtc).IsSuccess.Should().BeTrue();
        job.MarkInspectionPassed([]).IsSuccess.Should().BeTrue();
        return job;
    }

    [Fact]
    public async Task Sla_report_aggregates_per_lab_and_for_the_network()
    {
        var labA = Lab.Onboard("Xưởng A", "TP.HCM", 1);
        var machineA = MachineBuilder.Default(labA.Id);
        labA.AddMachine(machineA);

        var labB = Lab.Onboard("Xưởng B", "Hà Nội", 1);
        var machineB = MachineBuilder.Default(labB.Id);
        labB.AddMachine(machineB);

        var today = DateOnly.FromDateTime(Now);
        var onTimeA = CompletedJob(labA.Id, machineA.Id, today, Now.AddMinutes(-10));
        var lateA = CompletedJob(labA.Id, machineA.Id, today.AddDays(-1), Now.AddMinutes(-20));
        var onTimeB = CompletedJob(labB.Id, machineB.Id, today, Now.AddMinutes(-30));

        var labs = Substitute.For<ILabRepository>();
        var jobs = Substitute.For<IJobRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        labs.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { labA, labB });
        jobs.GetAllAssignedToLabsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { onTimeA, lateA, onTimeB });

        var handler = new GetNetworkSlaReportQueryHandler(labs, jobs, clock);
        var result = await handler.Handle(new GetNetworkSlaReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value;
        report.Labs.Should().HaveCount(2);
        report.WindowDays.Should().Be(90);

        var rowA = report.Labs.Single(x => x.LabId == labA.Id);
        rowA.CompletedJobs.Should().Be(2);
        rowA.AssignedJobs.Should().Be(2);
        rowA.LateJobs.Should().Be(1);
        rowA.OnTimeDeliveryRate.Should().Be(0.5m);
        rowA.FirstPassYield.Should().Be(1.0m);
        rowA.AcceptanceRate.Should().Be(1.0m);

        var rowB = report.Labs.Single(x => x.LabId == labB.Id);
        rowB.CompletedJobs.Should().Be(1);
        rowB.LateJobs.Should().Be(0);
        rowB.OnTimeDeliveryRate.Should().Be(1.0m);

        report.Network.LabId.Should().BeNull();
        report.Network.CompletedJobs.Should().Be(3);
        report.Network.AssignedJobs.Should().Be(3);
        report.Network.LateJobs.Should().Be(1);
        report.Network.OnTimeDeliveryRate.Should().Be(0.6667m, "2 of 3 completed jobs were on time");
    }

    [Fact]
    public async Task Lab_filter_narrows_rows_but_keeps_the_global_network_aggregate()
    {
        var labA = Lab.Onboard("Xưởng A", "TP.HCM", 1);
        var machineA = MachineBuilder.Default(labA.Id);
        labA.AddMachine(machineA);

        var labB = Lab.Onboard("Xưởng B", "Hà Nội", 1);
        var machineB = MachineBuilder.Default(labB.Id);
        labB.AddMachine(machineB);

        var today = DateOnly.FromDateTime(Now);
        var jobA = CompletedJob(labA.Id, machineA.Id, today, Now.AddMinutes(-10));
        var jobB = CompletedJob(labB.Id, machineB.Id, today.AddDays(-1), Now.AddMinutes(-20));

        var labs = Substitute.For<ILabRepository>();
        var jobs = Substitute.For<IJobRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        labs.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { labA, labB });
        jobs.GetAllAssignedToLabsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { jobA, jobB });

        var handler = new GetNetworkSlaReportQueryHandler(labs, jobs, clock);
        var result = await handler.Handle(new GetNetworkSlaReportQuery(labB.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LabFilter.Should().Be(labB.Id);
        result.Value.Labs.Should().ContainSingle(x => x.LabId == labB.Id);
        result.Value.Network.CompletedJobs.Should().Be(2, "the aggregate always covers the whole network");
        result.Value.Network.LateJobs.Should().Be(1);
    }

    [Fact]
    public async Task Unknown_lab_filter_returns_not_found()
    {
        var labs = Substitute.For<ILabRepository>();
        var jobs = Substitute.For<IJobRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        labs.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Lab>());

        var handler = new GetNetworkSlaReportQueryHandler(labs, jobs, clock);
        var result = await handler.Handle(new GetNetworkSlaReportQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
    }
}
