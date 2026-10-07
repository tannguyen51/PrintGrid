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

    private static Job CreateInProgressJob()
    {
        var job = CreateAssignedJob();
        job.Accept().IsSuccess.Should().BeTrue();
        job.Start(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        return job;
    }

    [Fact]
    public void Complete_requires_self_report_and_proof_photos()
    {
        var job = CreateInProgressJob();

        var result = job.Complete(DateTime.UtcNow, 75, "", []);

        result.IsFailure.Should().BeTrue();
        job.Status.Should().Be(JobStatus.InProgress);
    }

    [Fact]
    public void Complete_submits_qc_proof_for_staff_review()
    {
        var job = CreateInProgressJob();

        var result = job.Complete(DateTime.UtcNow, 75, "Dimensions and surface checked", ["qc/photo-1.jpg"]);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.AwaitingInspection);
        job.QcProofStatus.Should().Be(QcProofStatus.Pending);
        job.QcSelfReport.Should().Be("Dimensions and surface checked");
        job.GetQcProofPhotoKeys().Should().Equal("qc/photo-1.jpg");
    }

    [Fact]
    public void Approve_qc_proof_records_audit_and_unlocks_hub_inspection()
    {
        var job = CreateInProgressJob();
        job.Complete(DateTime.UtcNow, 75, "Checked", ["qc/photo.jpg"]);
        var staffId = Guid.NewGuid();
        var reviewedAt = DateTime.UtcNow;

        var result = job.ReviewQcProof(true, staffId, null, reviewedAt);

        result.IsSuccess.Should().BeTrue();
        job.QcProofStatus.Should().Be(QcProofStatus.Approved);
        job.QcReviewedBy.Should().Be(staffId);
        job.QcReviewedAtUtc.Should().Be(reviewedAt);
        job.Status.Should().Be(JobStatus.AwaitingInspection);
    }

    [Fact]
    public void Reject_qc_proof_requires_reason_and_returns_job_for_rework()
    {
        var job = CreateInProgressJob();
        job.Complete(DateTime.UtcNow, 75, "Checked", ["qc/photo.jpg"]);

        job.ReviewQcProof(false, Guid.NewGuid(), null, DateTime.UtcNow).IsFailure.Should().BeTrue();
        var result = job.ReviewQcProof(false, Guid.NewGuid(), "Image does not show dimensions", DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        job.QcProofStatus.Should().Be(QcProofStatus.Rejected);
        job.QcRejectionReason.Should().Be("Image does not show dimensions");
        job.Status.Should().Be(JobStatus.InProgress);
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
}
