using System.Data;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Scheduling;

/// <summary>Read-only proposals followed by an atomic, revalidated commit. Never changes the order's price.</summary>
public sealed class SplitDispatchService(PrintGridDbContext db, IMachineTimelineService timeline, IDateTimeProvider clock) : ISplitDispatchService
{
    private static readonly JobStatus[] Blocking = [JobStatus.Assigned, JobStatus.Accepted, JobStatus.InProgress];

    public async Task<Result<SplitDispatchPlan>> PreviewAsync(Guid jobId, CancellationToken ct)
    {
        var job = await db.Set<Job>().AsNoTracking().SingleOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null) return Result.Failure<SplitDispatchPlan>(Error.NotFound("Job", jobId));
        if (!await CanDispatch(job.OrderItemId, ct))
            return Result.Failure<SplitDispatchPlan>(Error.Conflict("Đơn phải còn hiệu lực và đã thanh toán đủ cọc trước khi phân công."));
        if (job.Status is not (JobStatus.Pending or JobStatus.Reassigned) || job.Quantity < 2)
            return Result.Failure<SplitDispatchPlan>(Error.Conflict("Chỉ có thể chia công việc chưa bắt đầu, số lượng từ 2."));
        var candidates = await Candidates(job, ct);
        var options = new List<(CapabilityCandidate Candidate, int Capacity)>();
        foreach (var candidate in candidates)
        {
            // Search whole batch windows; a one-unit gap does not imply capacity for a full batch.
            int low = 0, high = job.Quantity;
            while (low < high)
            {
                var qty = low + (high - low + 1) / 2;
                if (await Placement(job, candidate, qty, ct) is not null) low = qty;
                else high = qty - 1;
            }
            if (low > 0) options.Add((candidate, low));
        }
        var labs = options.GroupBy(o => o.Candidate.Lab.Id)
            .Select(g => g.OrderByDescending(o => o.Capacity).ThenBy(o => o.Candidate.Machine.Id).First())
            .OrderByDescending(o => o.Capacity).ThenBy(o => o.Candidate.Lab.Id).ToList();
        if (labs.Count < 2 || labs.Sum(o => (long)o.Capacity) < job.Quantity)
            return Result.Failure<SplitDispatchPlan>(Error.Conflict("Chưa có ít nhất hai lab tương thích đủ vật liệu và lịch trước hạn để chia đơn."));
        var batches = new List<DispatchBatch>();
        var remaining = job.Quantity;
        for (var i = 0; i < labs.Count && remaining > 0; i++)
        {
            var option = labs[i];
            var futureCapacity = labs.Skip(i + 1).Sum(o => (long)o.Capacity);
            var desired = (int)Math.Ceiling((decimal)remaining / (labs.Count - i));
            var qty = Math.Min(option.Capacity, Math.Max(desired, (int)Math.Max(0, remaining - futureCapacity)));
            qty = Math.Min(qty, remaining);
            var batch = await Placement(job, option.Candidate, qty, ct);
            if (batch is null) return Result.Failure<SplitDispatchPlan>(Error.Conflict("Lịch vừa thay đổi. Vui lòng tạo lại phương án."));
            batches.Add(batch);
            remaining -= qty;
        }
        if (remaining != 0 || batches.Count < 2)
            return Result.Failure<SplitDispatchPlan>(Error.Conflict("Không tìm được phương án chia đủ số lượng."));
        return Result.Success(new SplitDispatchPlan(job.Id, job.Quantity, job.InternalDueDate, clock.UtcNow.AddMinutes(5), batches));
    }

    public async Task<Result<IReadOnlyList<Guid>>> ConfirmAsync(SplitDispatchPlan plan, CancellationToken ct, string? actorId = null)
    {
        if (plan.ExpiresAtUtc <= clock.UtcNow)
            return Result.Failure<IReadOnlyList<Guid>>(Error.Conflict("Phương án đã hết hạn. Vui lòng xem lại phương án mới."));
        // Serializable + the existing machine-slot exclusion constraint guard concurrent confirmations.
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        try
        {
            var job = await db.Set<Job>().SingleOrDefaultAsync(j => j.Id == plan.JobId, ct);
            if (job is null) return Result.Failure<IReadOnlyList<Guid>>(Error.NotFound("Job", plan.JobId));
            if (!await CanDispatch(job.OrderItemId, ct)) return Stale();
            if (job.Status is not (JobStatus.Pending or JobStatus.Reassigned) || job.Quantity != plan.Quantity || job.InternalDueDate != plan.InternalDueDate
                || plan.Batches.Count < 2 || plan.Batches.Any(b => b.Quantity <= 0)
                || plan.Batches.Sum(b => (long)b.Quantity) != job.Quantity
                || plan.Batches.Select(b => b.LabId).Distinct().Count() != plan.Batches.Count)
                return Result.Failure<IReadOnlyList<Guid>>(Error.Conflict("Công việc đã thay đổi hoặc phương án không bảo toàn số lượng."));
            var candidates = await Candidates(job, ct);
            foreach (var batch in plan.Batches)
            {
                var candidate = candidates.SingleOrDefault(c => c.Lab.Id == batch.LabId && c.Machine.Id == batch.MachineId);
                if (candidate is null || batch.PlannedStartUtc < clock.UtcNow.AddSeconds(-30))
                    return Stale();
                var fresh = await Placement(job, candidate, batch.Quantity, ct, batch.PlannedStartUtc);
                if (fresh is null || fresh.PlannedStartUtc > batch.PlannedStartUtc.AddSeconds(30)
                    || Math.Abs((fresh.PlannedEndUtc - fresh.PlannedStartUtc - (batch.PlannedEndUtc - batch.PlannedStartUtc)).TotalSeconds) > 1)
                    return Stale();
                if (await db.Set<Job>().AnyAsync(j => j.MachineId == batch.MachineId && Blocking.Contains(j.Status)
                    && j.PlannedStartUtc < batch.PlannedEndUtc && j.PlannedEndUtc > batch.PlannedStartUtc, ct)) return Stale();
            }
            var split = job.Split(plan.Batches.Select(b => b.Quantity).ToList());
            if (split.IsFailure) return Result.Failure<IReadOnlyList<Guid>>(split.Error);
            for (var i = 0; i < split.Value.Count; i++)
            {
                var child = split.Value[i];
                var batch = plan.Batches[i];
                // The target uses the same verified material profile as the frozen quote.
                child.SetQuotedProduction(child.ProductionQuoteJson, job.QuotedMachineId.HasValue ? batch.MachineId : null);
                var assigned = child.AssignTo(batch.LabId, batch.MachineId, batch.PlannedStartUtc, batch.PlannedEndUtc, 0, clock.UtcNow);
                if (assigned.IsFailure) return Result.Failure<IReadOnlyList<Guid>>(assigned.Error);
                db.Set<AssignmentDecision>().Add(AssignmentDecision.Record(child, "ops_split", "assigned",
                    Application.Services.DecisionSnapshotJson.Candidates(new CapabilityFilterResult(candidates, [])),
                    "[]", "manual-split-v1", "{}", 0, 0, false,
                    reason: $"Ops confirmed {batch.Quantity}/{job.Quantity} units from parent {job.Id}",
                    actorType: "ops", actorId: actorId, chosenLabId: batch.LabId, chosenMachineId: batch.MachineId,
                    createdAtUtc: clock.UtcNow));
            }
            db.Set<Job>().AddRange(split.Value);
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Result.Success<IReadOnlyList<Guid>>(split.Value.Select(j => j.Id).ToList());
        }
        catch (Exception e) when (e is DbUpdateException || e is System.Data.Common.DbException)
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            return Stale();
        }
    }

    private static Result<IReadOnlyList<Guid>> Stale() => Result.Failure<IReadOnlyList<Guid>>(Error.Conflict("Lịch hoặc năng lực đã thay đổi. Vui lòng tạo lại phương án trước khi xác nhận."));

    private Task<bool> CanDispatch(Guid itemId, CancellationToken ct) => db.Set<PrintGrid.Modules.Customer.Domain.Entities.Order>()
        .AnyAsync(o => o.Items.Any(i => i.Id == itemId) && o.PaidAmount >= o.DepositAmount && o.PaidAmount > o.RefundedAmount
            && (o.Status == PrintGrid.Modules.Customer.Domain.Enums.OrderStatus.Confirmed || o.Status == PrintGrid.Modules.Customer.Domain.Enums.OrderStatus.InProduction), ct);

    private async Task<IReadOnlyList<CapabilityCandidate>> Candidates(Job job, CancellationToken ct)
    {
        var labs = await db.Set<Lab>().Include(l => l.Machines).Include(l => l.MaterialStocks).ToListAsync(ct);
        var counts = await db.Set<Job>().Where(j => j.LabId != null && Blocking.Contains(j.Status))
            .GroupBy(j => j.LabId!.Value).Select(g => new { LabId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.LabId, g => g.Count, ct);
        var spec = job.Specification;
        var first = job.MaterialByColor().First();
        var unit = JobSpecification.Create(BuildVolume.Create(spec.RequiredVolume.WidthMm, spec.RequiredVolume.DepthMm, spec.RequiredVolume.HeightMm),
            spec.MaterialCode, first.Key, spec.LayerHeightMm, spec.ToleranceMm, spec.Technology, Math.Max(.01m, first.Value / job.Quantity));
        var candidates = CapabilityFilter.Filter(unit, labs.Where(l => !l.MaxConcurrentJobs.HasValue || counts.GetValueOrDefault(l.Id) < l.MaxConcurrentJobs.Value)).Candidates;
        if (job.HasFrozenRequirements())
            return candidates.Where(c => Domain.Services.ProductionRequirementFilter.Reject(c.Machine, job.FrozenRequirements()) is null).ToList();
        if (!job.QuotedMachineId.HasValue) return candidates;
        var original = labs.SelectMany(l => l.Machines).SingleOrDefault(m => m.Id == job.QuotedMachineId);
        if (original is null) return [];
        var materialProfiles = await db.Set<CuraProfile>().Where(p => original.CuraProfileIds.Contains(p.Id) && p.MaterialCode == spec.MaterialCode && p.IsActive).Select(p => p.Id).ToListAsync(ct);
        return candidates.Where(c => c.Machine.Model.Equals(original.Model, StringComparison.OrdinalIgnoreCase)
            && c.Machine.CuraProfileIds.Any(materialProfiles.Contains)).ToList();
    }

    private async Task<DispatchBatch?> Placement(Job job, CapabilityCandidate candidate, int quantity, CancellationToken ct, DateTime? notBefore = null)
    {
        if (quantity < 1 || !job.MaterialByColor().All(d => candidate.Lab.HasStock(job.Specification.MaterialCode, d.Key, d.Value * quantity / job.Quantity))) return null;
        var baseline = Math.Max(1, (int)Math.Ceiling((decimal)job.EstimatedPrintMinutes * quantity / job.Quantity));
        var minutes = job.QuotedMachineId.HasValue ? baseline : candidate.Machine.ApplyToEstimate(baseline);
        // Timeline accepts baseline minutes and applies SpeedFactor internally. For frozen slicer
        // estimates compensate the input so the recorded duration stays equal to the quote.
        var duration = job.QuotedMachineId.HasValue ? minutes : minutes / candidate.Machine.SpeedFactor;
        var start = await timeline.FindEarliestFreeSlotAsync(candidate.Machine,
            (int)Math.Ceiling(duration * candidate.Machine.SpeedFactor), notBefore ?? clock.UtcNow.AddMinutes(5), ct);
        var end = start.AddMinutes((double)duration);
        var promised = await db.Set<PrintGrid.Modules.Customer.Domain.Entities.Order>()
            .Where(o => o.Items.Any(i => i.Id == job.OrderItemId)).Select(o => o.PromisedDeliveryDate).SingleAsync(ct);
        var deliveryDeadline = promised.AddDays(-(candidate.Lab.TransitDaysToHub + 1 + job.LastMileDays));
        var deadline = deliveryDeadline < job.InternalDueDate ? deliveryDeadline : job.InternalDueDate;
        if (end > deadline.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)) return null;
        if (candidate.Machine.MaintenanceFromUtc < end && candidate.Machine.MaintenanceUntilUtc > start) return null;
        return new DispatchBatch(candidate.Lab.Id, candidate.Lab.Name, candidate.Machine.Id, candidate.Machine.Name, quantity, start, end);
    }
}
