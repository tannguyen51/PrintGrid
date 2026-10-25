using MediatR;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.ShipmentBatches;

/// <summary>
/// FR-HUB-001 hub receipt (batch): the hub scans a sealed shipment batch and receives it.
/// Every job that was sealed into the batch moves from InTransit back to AwaitingInspection
/// so the hub QC step can start; the batch is closed as Received. The whole batch is
/// received atomically — a job in an unexpected state fails the command rather than leaving
/// the batch half-received.
/// </summary>
/// <param name="BatchId">Shipment batch being received.</param>
/// <param name="ReceivedByUserId">Hub user performing the receipt (recorded in the audit log).</param>
/// <param name="ScannedJobIds">
/// Optional scan reconciliation: when supplied, it must match the batch contents exactly.
/// </param>
public sealed record ReceiveShipmentBatchCommand(
    Guid BatchId,
    Guid? ReceivedByUserId,
    IReadOnlyList<Guid>? ScannedJobIds = null) : IRequest<Result<ReceiveShipmentBatchResultDto>>;

/// <summary>Outcome of one received batch, suitable for the hub receipt screen.</summary>
public sealed record ReceiveShipmentBatchResultDto(
    Guid BatchId,
    string Status,
    int ReceivedJobCount,
    DateTime ReceivedAtUtc,
    Guid? ReceivedByUserId);

public sealed class ReceiveShipmentBatchCommandHandler(
    IShipmentBatchRepository batches,
    IJobRepository jobs,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<ReceiveShipmentBatchCommandHandler> logger)
    : IRequestHandler<ReceiveShipmentBatchCommand, Result<ReceiveShipmentBatchResultDto>>
{
    public async Task<Result<ReceiveShipmentBatchResultDto>> Handle(
        ReceiveShipmentBatchCommand request,
        CancellationToken cancellationToken)
    {
        var batch = await batches.GetByIdAsync(request.BatchId, cancellationToken);
        if (batch is null)
            return Result.Failure<ReceiveShipmentBatchResultDto>(Error.NotFound("ShipmentBatch", request.BatchId));

        if (batch.Status != ShipmentBatchStatus.Sealed)
        {
            return Result.Failure<ReceiveShipmentBatchResultDto>(
                Error.Conflict($"Shipment batch in state {batch.Status} cannot be received"));
        }

        var batchJobs = await jobs.GetByShipmentBatchIdAsync(batch.Id, cancellationToken);

        if (request.ScannedJobIds is { } scanned)
        {
            var scannedSet = scanned.ToHashSet();
            if (scannedSet.Count != scanned.Count)
            {
                return Result.Failure<ReceiveShipmentBatchResultDto>(
                    Error.Validation("The scanned job list contains duplicate job ids"));
            }

            var expected = batchJobs.Select(job => job.Id).ToHashSet();
            var missing = expected.Except(scannedSet).ToList();
            var unexpected = scannedSet.Except(expected).ToList();
            if (missing.Count > 0 || unexpected.Count > 0)
            {
                return Result.Failure<ReceiveShipmentBatchResultDto>(Error.Validation(
                    $"Scanned jobs do not match the batch: {missing.Count} missing ({Format(missing)}), " +
                    $"{unexpected.Count} unexpected ({Format(unexpected)})"));
            }
        }

        if (batchJobs.Count != batch.JobCount)
        {
            // Data drift is worth a signal, but the jobs actually sealed into the batch are
            // still the source of truth for the receipt.
            logger.LogWarning(
                "Shipment batch {BatchId} declares {DeclaredJobCount} jobs but contains {ActualJobCount}",
                batch.Id, batch.JobCount, batchJobs.Count);
        }

        var now = clock.UtcNow;

        var received = batch.MarkReceived(now);
        if (received.IsFailure)
            return Result.Failure<ReceiveShipmentBatchResultDto>(received.Error);

        foreach (var job in batchJobs)
        {
            var jobReceived = job.ReceiveAtHub();
            if (jobReceived.IsFailure)
                return Result.Failure<ReceiveShipmentBatchResultDto>(jobReceived.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Shipment batch {BatchId} ({WaybillNumber}) received at the hub by {ReceivedByUserId}: {JobCount} job(s) moved to AwaitingInspection",
            batch.Id, batch.WaybillNumber, request.ReceivedByUserId ?? Guid.Empty, batchJobs.Count);

        return Result.Success(new ReceiveShipmentBatchResultDto(
            batch.Id,
            batch.Status.ToString(),
            batchJobs.Count,
            now,
            request.ReceivedByUserId));
    }

    private static string Format(IReadOnlyList<Guid> ids) =>
        ids.Count == 0 ? "none" : string.Join(", ", ids);
}
