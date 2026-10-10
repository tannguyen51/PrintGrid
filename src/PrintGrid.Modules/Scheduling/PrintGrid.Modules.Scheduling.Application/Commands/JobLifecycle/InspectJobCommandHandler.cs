using MediatR;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Scheduling.Application.Commands.UrgentReprint;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

/// <summary>
/// Hub QC decision (FR-HUB-002). PASS closes the job; FAIL must carry photographic evidence,
/// a fully evaluated checklist and a fault attribution (BR-QC-002/003/004):
/// <list type="bullet">
/// <item>customer fault — the job is closed as failed, NO reprint is created and the customer
/// is told the model file is the problem (FR-HUB-002 AC02);</item>
/// <item>lab/hub fault — the failure is recorded and handed to the urgent-reprint flow
/// (FR-HUB-003), which owns the URGENT priority, the inherited deadline, the cost
/// attribution, the cap of two reprints and the decision log.</item>
/// </list>
/// </summary>
public class InspectJobCommandHandler : IRequestHandler<InspectJobCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;
    private readonly ILogger<InspectJobCommandHandler> _logger;

    public InspectJobCommandHandler(
        IJobRepository jobs,
        IUnitOfWork unitOfWork,
        ISender sender,
        ILogger<InspectJobCommandHandler> logger)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
        _sender = sender;
        _logger = logger;
    }

    public async Task<Result> Handle(InspectJobCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        // Only a job awaiting inspection can be judged — this also stops a retried FAIL from
        // creating a second reprint for the same failed print.
        if (job.Status != JobStatus.AwaitingInspection)
            return Result.Failure(Error.Conflict($"Job in state {job.Status} cannot be inspected"));

        // AC01/AC02 (BR-QC-002/003): no conclusion without evidence, none with a half-done checklist.
        if (command.PhotoUrls is null || command.PhotoUrls.Count == 0)
            return Result.Failure(Error.Validation("Photographic evidence is required for inspection"));
        if (command.ChecklistResults is null || command.ChecklistResults.Count == 0)
            return Result.Failure(Error.Validation("Inspection checklist items are mandatory"));

        if (command.Passed)
        {
            var passed = job.MarkInspectionPassed(command.PhotoUrls.ToList());
            if (passed.IsFailure) return passed;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // AC03 (BR-QC-004): a failure without attribution cannot be routed to anyone.
        if (command.FaultAttribution is null)
            return Result.Failure(Error.Validation(
                "Fault attribution (Lab, Hub or Customer) is required when inspection fails"));

        var fault = command.FaultAttribution.Value;
        var reason = command.Note ?? $"Failed hub inspection ({fault} fault)";

        if (fault == FaultAttribution.Customer)
        {
            // AC02: the customer's own file is at fault — no reprint job, and the customer is told.
            var message =
                $"Chi tiết in '{job.Specification.MaterialCode}' không đạt kiểm chuẩn do vấn đề mô hình file 3D từ khách hàng. " +
                "Vui lòng liên hệ để xác nhận in lại có phí hoặc hoàn tiền.";

            var customerFault = job.FailInspectionCustomerFault(
                reason, command.PhotoUrls.ToList(), message);
            if (customerFault.IsFailure) return customerFault;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Job {JobId} failed inspection attributed to the customer — no reprint created",
                job.Id);
            return Result.Success();
        }

        var failedAtLabId = job.LabId;

        var fail = job.Fail(reason);
        if (fail.IsFailure) return fail;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var reprint = await _sender.Send(
            new CreateUrgentReprintCommand(job.Id, reason, Fault: fault),
            cancellationToken);

        if (reprint.IsFailure)
        {
            // The QC failure itself is committed; what failed is the reprint, which ops must
            // pick up. Surfacing it lets the hub see why no reprint job appeared.
            _logger.LogError(
                "QC failure recorded for job {JobId} but the urgent reprint could not be created: {Code} {Message}",
                job.Id, reprint.Error.Code, reprint.Error.Message);
            return Result.Failure(reprint.Error);
        }

        if (reprint.Value.EscalatedToOps)
        {
            _logger.LogWarning(
                "Job {JobId} failed inspection again; reprint limit reached, escalated to operations",
                job.Id);
        }
        else
        {
            _logger.LogInformation(
                "Job {JobId} failed inspection ({Fault} fault) — reprint {ReprintJobId} created, charged to the at-fault party; lab of the failed print was {FailedAtLabId}",
                job.Id, fault, reprint.Value.ReprintJobId, failedAtLabId);
        }

        return Result.Success();
    }
}
