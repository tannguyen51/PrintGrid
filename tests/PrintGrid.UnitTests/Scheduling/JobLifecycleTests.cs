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
    public void Job_FailInspectionWithReprint_creates_events_and_fails_job_FR_HUB_003_AC01()
    {
        var job = CreateAssignedJob();
        job.Accept();
        job.Start(DateTime.UtcNow);
        job.Complete(DateTime.UtcNow, 60);

        var reprintJobId = Guid.NewGuid();
        var photoUrls = new[] { "https://cdn.printgrid.dev/qc/defect.jpg" };

        var result = job.FailInspectionWithReprint(reprintJobId, FaultAttribution.Lab, "Bề mặt nứt", photoUrls);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Failed);
        job.FailureReason.Should().Be("Bề mặt nứt");

        job.DomainEvents.Should().ContainSingle(e => e is JobInspectionFailedReprintTriggeredEvent);
        var reprintEvt = job.DomainEvents.OfType<JobInspectionFailedReprintTriggeredEvent>().Single();
        reprintEvt.ReprintJobId.Should().Be(reprintJobId);
        reprintEvt.FaultAttribution.Should().Be("Lab");
        reprintEvt.IsUrgent.Should().BeTrue();
    }

    [Fact]
    public void Job_FailInspectionCustomerFault_emits_notification_event_FR_HUB_003_AC02()
    {
        var job = CreateAssignedJob();
        job.Accept();
        job.Start(DateTime.UtcNow);
        job.Complete(DateTime.UtcNow, 60);

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
        job.Complete(DateTime.UtcNow, 60);

        var photoUrls = new[] { "https://cdn.printgrid.dev/qc/ok.jpg" };
        var result = job.MarkInspectionPassed(photoUrls);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Completed);
        job.DomainEvents.Should().ContainSingle(e => e is JobInspectionPassedEvent);
    }
}

