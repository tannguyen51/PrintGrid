using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Analytics.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Api.Finance;

/// <summary>
/// A lab-funded replacement fulfils the original production entitlement. It only loses the
/// portion already earned by linked earlier attempts, never its entire fee merely for being a reprint.
/// Load the whole lineage before date/lab filters so historical earnings cannot be paid twice.
/// </summary>
public sealed class JobEarningsCalculator
{
    private readonly Dictionary<Guid, Job> _jobs;
    private readonly Dictionary<Guid, LabPayable> _payables;
    private readonly Dictionary<Guid, (decimal Total, int Quantity)> _lines;
    private readonly Dictionary<Guid, JobEarnings> _calculated = [];
    private readonly HashSet<Guid> _calculating = [];

    private JobEarningsCalculator(IEnumerable<Job> jobs, IEnumerable<LabPayable> payables,
        Dictionary<Guid, (decimal Total, int Quantity)> lines)
    {
        _jobs = jobs.ToDictionary(j => j.Id);
        _payables = payables.ToDictionary(p => p.JobId);
        _lines = lines;
    }

    public static async Task<JobEarningsCalculator> LoadAsync(PrintGridDbContext db,
        IReadOnlyCollection<Guid> orderItemIds, CancellationToken ct)
    {
        var jobs = await db.Set<Job>().AsNoTracking().Where(j => orderItemIds.Contains(j.OrderItemId)).ToListAsync(ct);
        var jobIds = jobs.Select(j => j.Id).ToList();
        var payables = await db.Set<LabPayable>().AsNoTracking().Where(p => jobIds.Contains(p.JobId)).ToListAsync(ct);
        var lines = await db.Set<OrderItem>().AsNoTracking().Where(i => orderItemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Quantity, Total = i.BatchTotalAmount ?? i.UnitPrice.Amount * i.Quantity }).ToListAsync(ct);
        return new JobEarningsCalculator(jobs, payables, lines.ToDictionary(i => i.Id, i => (i.Total, i.Quantity)));
    }

    public JobEarnings Calculate(Job job)
    {
        if (_calculated.TryGetValue(job.Id, out var cached)) return cached;
        if (!_calculating.Add(job.Id)) throw new InvalidOperationException("A reprint lineage contains a cycle.");
        try
        {
            _payables.TryGetValue(job.Id, out var payable);
            // Preserve actual payments. A historical zero-value "Paid" marker may come from the
            // former blanket reprint deduction and must still be checked against the lineage.
            if (payable?.Status == "Paid" && payable.NetAmount != 0m)
                return _calculated[job.Id] = new(payable.GrossAmount, payable.DeductionAmount);

            var line = _lines[job.OrderItemId];
            var gross = payable?.GrossAmount
                ?? LabRevenueShare.Calculate(line.Total, line.Quantity, job.Quantity).LabAmount;
            var ownFault = job.CostBearer == CostBearer.Lab && (job.FaultLabId ?? job.LabId) == job.LabId;
            if (!ownFault)
                return _calculated[job.Id] = new(gross, payable?.DeductionAmount ?? 0m);

            decimal previouslyEarned = 0;
            foreach (var ancestor in Ancestors(job))
            {
                // A failed attempt with no payable has earned no production fee.
                if (ancestor.Status != JobStatus.Completed && !_payables.ContainsKey(ancestor.Id)) continue;
                var earned = Calculate(ancestor).NetAmount;
                // A replacement may itself be split. Only consume the corresponding quantity share.
                previouslyEarned += earned * job.Quantity / ancestor.Quantity;
            }
            var deduction = Math.Min(gross, Math.Max(0m, decimal.Round(previouslyEarned, 2)));
            return _calculated[job.Id] = new(gross, deduction);
        }
        finally { _calculating.Remove(job.Id); }
    }

    private IEnumerable<Job> Ancestors(Job job)
    {
        var visited = new HashSet<Guid> { job.Id };
        var current = job;
        while (true)
        {
            // Split children point to their batch parent; replacements point to the exact attempt.
            // OriginalJobId is a fallback for older unsplit attempts without ReprintOfJobId.
            var parentId = current.ReprintOfJobId ?? current.ParentJobId
                ?? (current.ReprintIndex > 0 ? current.OriginalJobId : null);
            if (parentId is null || !visited.Add(parentId.Value)
                || !_jobs.TryGetValue(parentId.Value, out var parent) || parent.OrderItemId != job.OrderItemId)
                yield break;
            yield return parent;
            current = parent;
        }
    }
}

public sealed record JobEarnings(decimal GrossAmount, decimal DeductionAmount)
{
    public decimal NetAmount => GrossAmount - DeductionAmount;
}
