using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class MaterialReservation : Entity<Guid>
{
    public Guid LabId { get; private set; }
    public Guid MaterialStockId { get; private set; }
    public Guid JobId { get; private set; }
    public decimal ReservedGrams { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private MaterialReservation() { }

    internal static MaterialReservation Create(Guid labId, Guid stockId, Guid jobId, decimal reservedGrams) => new()
    {
        Id = Guid.NewGuid(), LabId = labId, MaterialStockId = stockId, JobId = jobId,
        ReservedGrams = reservedGrams, CreatedAtUtc = DateTime.UtcNow
    };
}
