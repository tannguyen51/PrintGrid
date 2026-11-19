using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Abstractions;

public sealed record DispatchBatch(Guid LabId, string LabName, Guid MachineId, string MachineName,
    int Quantity, DateTime PlannedStartUtc, DateTime PlannedEndUtc);
public sealed record SplitDispatchPlan(Guid JobId, int Quantity, DateOnly InternalDueDate,
    DateTime ExpiresAtUtc, IReadOnlyList<DispatchBatch> Batches);
public interface ISplitDispatchService
{
    Task<Result<SplitDispatchPlan>> PreviewAsync(Guid jobId, CancellationToken ct);
    Task<Result<IReadOnlyList<Guid>>> ConfirmAsync(SplitDispatchPlan plan, CancellationToken ct, string? actorId = null);
}
