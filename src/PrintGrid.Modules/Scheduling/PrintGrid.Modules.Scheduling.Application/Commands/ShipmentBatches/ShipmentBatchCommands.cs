using MediatR;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.ShipmentBatches;

public sealed record ShipmentJobDto(
    Guid Id,
    DateOnly InternalDueDate,
    decimal WeightGrams,
    int Quantity,
    string MaterialCode,
    DateTime? ProofApprovedAtUtc);

public sealed record ShipmentBatchDto(
    Guid Id,
    DateOnly ScheduledTransferDate,
    string WaybillNumber,
    string Status,
    int JobCount,
    decimal TotalWeightGrams,
    decimal TransportAllowance,
    string Currency,
    DateTime SealedAtUtc);

public sealed record ShipmentPlannerDto(
    Guid LabId,
    string LabName,
    string TransportZone,
    string TransferDaysOfWeek,
    DateOnly NextTransferDate,
    IReadOnlyList<ShipmentJobDto> EligibleJobs,
    IReadOnlyList<ShipmentBatchDto> RecentBatches);

public sealed record GetShipmentPlannerQuery(Guid LabId) : IRequest<Result<ShipmentPlannerDto>>;
public sealed record PreviewShipmentBatchQuery(Guid LabId, IReadOnlyList<Guid> JobIds) : IRequest<Result<TransportAllowanceQuote>>;
public sealed record SealShipmentBatchCommand(Guid LabId, IReadOnlyList<Guid> JobIds, Guid SealedBy) : IRequest<Result<ShipmentBatchDto>>;

internal static class ShipmentWeight
{
    public static decimal Of(Job job) => job.ActualMaterialGrams ?? job.Specification.MaterialGrams;
}

public sealed class GetShipmentPlannerQueryHandler(
    ILabRepository labs,
    IJobRepository jobs,
    IShipmentBatchRepository batches,
    IDateTimeProvider clock)
    : IRequestHandler<GetShipmentPlannerQuery, Result<ShipmentPlannerDto>>
{
    public async Task<Result<ShipmentPlannerDto>> Handle(GetShipmentPlannerQuery request, CancellationToken ct)
    {
        var lab = await labs.GetByIdAsync(request.LabId, ct);
        if (lab is null) return Result.Failure<ShipmentPlannerDto>(Error.NotFound("Lab", request.LabId));
        var eligible = await jobs.GetProofApprovedForShipmentAsync(request.LabId, ct);
        var recent = await batches.GetByLabAsync(request.LabId, ct);
        return Result.Success(new ShipmentPlannerDto(
            lab.Id, lab.Name, lab.TransportZone, lab.TransferDaysOfWeek, lab.NextTransferDate(clock.Today),
            eligible.Select(job => new ShipmentJobDto(job.Id, job.InternalDueDate, ShipmentWeight.Of(job), job.Quantity, job.Specification.MaterialCode, job.QcReviewedAtUtc)).ToList(),
            recent.Take(10).Select(batch => new ShipmentBatchDto(batch.Id, batch.ScheduledTransferDate, batch.WaybillNumber, batch.Status.ToString(), batch.JobCount, batch.TotalWeightGrams, batch.TransportAllowance, batch.Currency, batch.SealedAtUtc)).ToList()));
    }
}

public sealed class PreviewShipmentBatchQueryHandler(
    ILabRepository labs,
    IJobRepository jobs,
    TransportAllowancePolicy policy)
    : IRequestHandler<PreviewShipmentBatchQuery, Result<TransportAllowanceQuote>>
{
    public async Task<Result<TransportAllowanceQuote>> Handle(PreviewShipmentBatchQuery request, CancellationToken ct)
    {
        if (request.JobIds.Count == 0 || request.JobIds.Count != request.JobIds.Distinct().Count())
            return Result.Failure<TransportAllowanceQuote>(Error.Validation("Select at least one unique job"));
        var lab = await labs.GetByIdAsync(request.LabId, ct);
        if (lab is null) return Result.Failure<TransportAllowanceQuote>(Error.NotFound("Lab", request.LabId));
        var selected = await jobs.GetByIdsAsync(request.JobIds, ct);
        if (selected.Count != request.JobIds.Count || selected.Any(job => job.LabId != request.LabId || job.Status != JobStatus.AwaitingInspection || job.QcProofStatus != QcProofStatus.Approved || job.ShipmentBatchId.HasValue))
            return Result.Failure<TransportAllowanceQuote>(Error.Conflict("Selection contains a job that is not eligible for shipment"));
        try { return Result.Success(policy.Quote(lab.TransportZone, selected.Sum(ShipmentWeight.Of))); }
        catch (InvalidOperationException ex) { return Result.Failure<TransportAllowanceQuote>(Error.Validation(ex.Message)); }
    }
}

public sealed class SealShipmentBatchCommandHandler(
    ILabRepository labs,
    IJobRepository jobs,
    IShipmentBatchRepository batches,
    TransportAllowancePolicy policy,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SealShipmentBatchCommand, Result<ShipmentBatchDto>>
{
    public async Task<Result<ShipmentBatchDto>> Handle(SealShipmentBatchCommand request, CancellationToken ct)
    {
        var preview = await new PreviewShipmentBatchQueryHandler(labs, jobs, policy)
            .Handle(new PreviewShipmentBatchQuery(request.LabId, request.JobIds), ct);
        if (preview.IsFailure) return Result.Failure<ShipmentBatchDto>(preview.Error);
        var lab = await labs.GetByIdAsync(request.LabId, ct);
        var selected = await jobs.GetByIdsAsync(request.JobIds, ct);
        var quote = preview.Value;
        var batch = ShipmentBatch.Seal(request.LabId, lab!.NextTransferDate(clock.Today), quote.Zone, quote.WeightClass,
            quote.TotalWeightGrams, quote.ZoneRate, quote.WeightMultiplier, quote.Allowance, selected.Count, request.SealedBy, clock.UtcNow);
        foreach (var job in selected)
        {
            var result = job.SealForHubShipment(batch.Id);
            if (result.IsFailure) return Result.Failure<ShipmentBatchDto>(result.Error);
        }
        await batches.AddAsync(batch, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new ShipmentBatchDto(batch.Id, batch.ScheduledTransferDate, batch.WaybillNumber, batch.Status.ToString(), selected.Count, batch.TotalWeightGrams, batch.TransportAllowance, batch.Currency, batch.SealedAtUtc));
    }
}
