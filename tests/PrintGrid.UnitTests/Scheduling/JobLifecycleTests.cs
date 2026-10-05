using FluentAssertions;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Events;
using Xunit;

namespace PrintGrid.UnitTests.Scheduling;

public class JobLifecycleTests
{
    private static Job CreateAssignedJob(DateTime? assignedAtUtc = null)
    {
        var job = Job.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SpecBuilder.Default(),
            estimatedPrintMinutes: 60,
            internalDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));

        var labId = Guid.NewGuid();
        var machineId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddHours(1);
        var end = start.AddHours(2);

        job.AssignTo(labId, machineId, start, end, 0.95m, assignedAtUtc);
        return job;
    }

    [Fact]
    public void Decline_transitions_job_back_to_pending_and_clears_assignment()
    {
        var job = CreateAssignedJob();
        var reason = "Hết vật liệu";

        var result = job.Decline(reason);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Pending);
        job.LabId.Should().BeNull();
        job.MachineId.Should().BeNull();
        job.PlannedStartUtc.Should().BeNull();
        job.PlannedEndUtc.Should().BeNull();
        job.AssignedAtUtc.Should().BeNull();
        job.FailureReason.Should().Be(reason);

        job.DomainEvents.Should().ContainSingle(e => e is JobDeclinedEvent);
        var declinedEvent = job.DomainEvents.OfType<JobDeclinedEvent>().Single();
        declinedEvent.Reason.Should().Be(reason);

        job.DomainEvents.Should().ContainSingle(e => e is ReschedulingTriggeredEvent);
    }

    [Fact]
    public void Accept_within_two_hours_succeeds()
    {
        var assignedAt = DateTime.UtcNow.AddHours(-1); // 1 hour ago
        var job = CreateAssignedJob(assignedAt);

        var result = job.Accept(DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Accepted);
    }

    [Fact]
    public void Accept_after_two_hours_fails_and_auto_declines_to_pending()
    {
        var assignedAt = DateTime.UtcNow.AddHours(-2.5); // 2.5 hours ago
        var job = CreateAssignedJob(assignedAt);

        var result = job.Accept(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Pending);
        job.FailureReason.Should().Contain("expired");
    }

    [Fact]
    public void TimeoutAcceptance_after_two_hours_auto_declines_to_pending()
    {
        var assignedAt = DateTime.UtcNow.AddHours(-2.1);
        var job = CreateAssignedJob(assignedAt);

        var result = job.TimeoutAcceptance(DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Pending);
        job.LabId.Should().BeNull();
    }

    [Fact]
    public void TimeoutAcceptance_before_two_hours_fails()
    {
        var assignedAt = DateTime.UtcNow.AddMinutes(-30);
        var job = CreateAssignedJob(assignedAt);

        var result = job.TimeoutAcceptance(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Assigned);
    }

    [Fact]
    public void Completion_requires_positive_actual_time_and_material()
    {
        var job = CreateAssignedJob();
        job.Accept().IsSuccess.Should().BeTrue();
        job.Start(DateTime.UtcNow).IsSuccess.Should().BeTrue();

        job.Complete(DateTime.UtcNow, 0, 10m).IsFailure.Should().BeTrue();
        job.Complete(DateTime.UtcNow, 60, 0m).IsFailure.Should().BeTrue();
        job.Complete(DateTime.UtcNow, 60, 42m).IsSuccess.Should().BeTrue();
        job.ActualPrintMinutes.Should().Be(60);
        job.ActualMaterialGrams.Should().Be(42m);
    }
}
