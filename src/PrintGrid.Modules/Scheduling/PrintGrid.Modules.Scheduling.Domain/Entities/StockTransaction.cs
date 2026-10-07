using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class StockTransaction : Entity<Guid>
{
    public Guid LabId { get; private set; }
    public Guid MaterialStockId { get; private set; }
    public string TransactionCode { get; private set; } = string.Empty;
    public decimal DeltaGrams { get; private set; }
    public decimal RunningTotalGrams { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid? JobId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private StockTransaction() { }

    internal static StockTransaction Create(Guid labId, Guid stockId, decimal deltaGrams, decimal runningTotalGrams, string reason, Guid? jobId = null)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Transaction reason is required", nameof(reason));
        return new StockTransaction
        {
            Id = Guid.NewGuid(), LabId = labId, MaterialStockId = stockId,
            TransactionCode = $"STK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25].ToUpperInvariant(),
            DeltaGrams = deltaGrams, RunningTotalGrams = runningTotalGrams,
            Reason = reason.Trim(), JobId = jobId, CreatedAtUtc = DateTime.UtcNow
        };
    }
}
