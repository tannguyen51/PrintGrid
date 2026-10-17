using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public enum ShipmentBatchStatus
{
    Sealed,
    Received
}

public class ShipmentBatch : AggregateRoot<Guid>
{
    public Guid LabId { get; private set; }
    public DateOnly ScheduledTransferDate { get; private set; }
    public string TransportZone { get; private set; } = string.Empty;
    public string WeightClass { get; private set; } = string.Empty;
    public decimal TotalWeightGrams { get; private set; }
    public decimal ZoneRate { get; private set; }
    public decimal WeightMultiplier { get; private set; }
    public decimal TransportAllowance { get; private set; }
    public int JobCount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public string WaybillNumber { get; private set; } = string.Empty;
    public ShipmentBatchStatus Status { get; private set; }
    public DateTime SealedAtUtc { get; private set; }
    public Guid SealedBy { get; private set; }
    public DateTime? ReceivedAtUtc { get; private set; }

    private ShipmentBatch() { }

    public static ShipmentBatch Seal(
        Guid labId,
        DateOnly scheduledTransferDate,
        string transportZone,
        string weightClass,
        decimal totalWeightGrams,
        decimal zoneRate,
        decimal weightMultiplier,
        decimal transportAllowance,
        int jobCount,
        Guid sealedBy,
        DateTime nowUtc)
    {
        if (totalWeightGrams <= 0) throw new ArgumentOutOfRangeException(nameof(totalWeightGrams));
        if (transportAllowance < 0) throw new ArgumentOutOfRangeException(nameof(transportAllowance));
        if (jobCount <= 0) throw new ArgumentOutOfRangeException(nameof(jobCount));
        var id = Guid.NewGuid();
        return new ShipmentBatch
        {
            Id = id,
            LabId = labId,
            ScheduledTransferDate = scheduledTransferDate,
            TransportZone = transportZone,
            WeightClass = weightClass,
            TotalWeightGrams = decimal.Round(totalWeightGrams, 2),
            ZoneRate = zoneRate,
            WeightMultiplier = weightMultiplier,
            TransportAllowance = decimal.Round(transportAllowance, 0),
            JobCount = jobCount,
            WaybillNumber = $"PG-{scheduledTransferDate:yyyyMMdd}-{id.ToString("N")[..8].ToUpperInvariant()}",
            Status = ShipmentBatchStatus.Sealed,
            SealedAtUtc = nowUtc,
            SealedBy = sealedBy
        };
    }

    public Result MarkReceived(DateTime nowUtc)
    {
        if (Status != ShipmentBatchStatus.Sealed)
            return Result.Failure(Error.Conflict("Shipment batch has already been received"));
        Status = ShipmentBatchStatus.Received;
        ReceivedAtUtc = nowUtc;
        return Result.Success();
    }
}
