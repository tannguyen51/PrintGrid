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

    [Fact]
    public void Validator_fails_when_no_photo_evidence_provided_AC01()
    {
        var validator = new PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle.InspectJobCommandValidator();
        var command = new PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle.InspectJobCommand(
            Guid.NewGuid(),
            Passed: true,
            ChecklistResults: new[] { new PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle.ChecklistItemResult("Kích thước", "Pass") },
            PhotoUrls: Array.Empty<string>()
        );

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhotoUrls");
    }

    [Fact]
    public void Validator_fails_when_checklist_item_has_invalid_status_AC02()
    {
        var validator = new PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle.InspectJobCommandValidator();
        var command = new PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle.InspectJobCommand(
            Guid.NewGuid(),
            Passed: true,
            ChecklistResults: new[] { new PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle.ChecklistItemResult("Kích thước", "InvalidStatus") },
            PhotoUrls: new[] { "https://cdn.printgrid.dev/qc/sample.jpg" }
        );

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("PASS, FAIL, or NotApplicable"));
    }

    [Fact]
    public void Validator_fails_when_failed_inspection_lacks_fault_attribution_AC03()
    {
        var validator = new PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle.InspectJobCommandValidator();
        var command = new PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle.InspectJobCommand(
            Guid.NewGuid(),
            Passed: false,
            ChecklistResults: new[] { new PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle.ChecklistItemResult("Kích thước", "Fail") },
            PhotoUrls: new[] { "https://cdn.printgrid.dev/qc/defect.jpg" },
            FaultAttribution: null
        );

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FaultAttribution");
    }

    [Fact]
    public void Job_Fail_records_the_failure_and_asks_for_a_replan_FR_HUB_003_AC01()
    {
        var job = CreateAssignedJob();
        job.Accept();
        job.Start(DateTime.UtcNow);
        job.Complete(DateTime.UtcNow, 60, "Bề mặt đạt", new[] { "qc/ok.jpg" });

        var result = job.Fail("Bề mặt nứt");

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Failed);
        job.FailureReason.Should().Be("Bề mặt nứt");

        // The urgent reprint itself (URGENT, inherited deadline, cost bearer, cap of two) is
        // created by CreateUrgentReprintCommand — see UrgentReprintTests and the
        // RescheduleAndReprintFlowTests integration run. The aggregate's part is recording the
        // failure and asking for a replan.
        job.DomainEvents.Should().ContainSingle(e => e is JobFailedEvent);
        job.DomainEvents.OfType<ReschedulingTriggeredEvent>().Single().Trigger.Should().Be("print_failure");
    }

    [Fact]
    public void Job_FailInspectionCustomerFault_emits_notification_event_FR_HUB_003_AC02()
    {
        var job = CreateAssignedJob();
        job.Accept();
        job.Start(DateTime.UtcNow);
        job.Complete(DateTime.UtcNow, 60, "Bề mặt đạt", new[] { "qc/ok.jpg" });

        var photoUrls = new[] { "https://cdn.printgrid.dev/qc/defect.jpg" };
        var message = "Lỗi file 3D của khách hàng";

        var result = job.FailInspectionCustomerFault("Lỗi mesh", photoUrls, message);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Failed);

        job.DomainEvents.Should().ContainSingle(e => e is CustomerFaultInspectionFailedEvent);
        var custEvt = job.DomainEvents.OfType<CustomerFaultInspectionFailedEvent>().Single();
        custEvt.CustomerNotificationMessage.Should().Be(message);
    }

    [Fact]
    public void Job_MarkInspectionPassed_completes_job_and_emits_event()
    {
        var job = CreateAssignedJob();
        job.Accept();
        job.Start(DateTime.UtcNow);
        job.Complete(DateTime.UtcNow, 60, "Bề mặt đạt", new[] { "qc/ok.jpg" });
        // The lab's own QC evidence must be approved before the hub signs the job off.
        job.ReviewQcProof(approved: true, Guid.NewGuid(), null, DateTime.UtcNow);

        var photoUrls = new[] { "https://cdn.printgrid.dev/qc/ok.jpg" };
        var result = job.MarkInspectionPassed(photoUrls);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Completed);
        job.DomainEvents.Should().ContainSingle(e => e is JobInspectionPassedEvent);
    }

    [Fact]
    public void Job_MarkInspectionPassed_is_refused_until_the_lab_proof_is_approved()
    {
        var job = CreateAssignedJob();
        job.Accept();
        job.Start(DateTime.UtcNow);
        job.Complete(DateTime.UtcNow, 60, "Bề mặt đạt", new[] { "qc/ok.jpg" });

        var result = job.MarkInspectionPassed(new[] { "https://cdn.printgrid.dev/qc/ok.jpg" });

        result.IsFailure.Should().BeTrue(
            "the hub only signs off on work whose lab QC evidence has been approved");
        job.Status.Should().Be(JobStatus.AwaitingInspection);
    }
}

