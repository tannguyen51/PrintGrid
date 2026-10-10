using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class Lab : AggregateRoot<Guid>
{
    private readonly List<Machine> _machines = new();
    private readonly List<MaterialStock> _materialStocks = new();
    private readonly List<StockTransaction> _stockTransactions = new();
    private readonly List<MaterialReservation> _materialReservations = new();

    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public decimal OnTimeDeliveryRate { get; private set; } = 1.0m;
    public decimal FirstPassYield { get; private set; } = 1.0m;
    public int TransitDaysToHub { get; private set; }
    public bool IsInGoodStanding { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<Machine> Machines => _machines.AsReadOnly();
    public IReadOnlyCollection<MaterialStock> MaterialStocks => _materialStocks.AsReadOnly();
    public IReadOnlyCollection<StockTransaction> StockTransactions => _stockTransactions.AsReadOnly();
    public IReadOnlyCollection<MaterialReservation> MaterialReservations => _materialReservations.AsReadOnly();

    private Lab() { }

    public static Lab Onboard(string name, string city, int transitDaysToHub)
    {
        if (transitDaysToHub < 0)
            throw new ArgumentOutOfRangeException(nameof(transitDaysToHub), "Transit days cannot be negative");

        return new Lab
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            City = city.Trim(),
            TransitDaysToHub = transitDaysToHub,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddMachine(Machine machine) => _machines.Add(machine);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void SuspendFromAssignments() => IsInGoodStanding = false;

    public void RestoreGoodStanding() => IsInGoodStanding = true;

    public void SetMaterialStock(string materialCode, string colorCode, decimal availableGrams, decimal reorderPointGrams = 0m)
    {
        var material = NormalizeStockCode(materialCode);
        var color = NormalizeStockCode(colorCode);
        var existing = _materialStocks.SingleOrDefault(s => s.MaterialCode == material && s.ColorCode == color);
        if (existing is null)
        {
            existing = MaterialStock.Create(Id, material, color, 0m, reorderPointGrams);
            _materialStocks.Add(existing);
        }
        else existing.ConfigureReorderPoint(reorderPointGrams);

        var delta = availableGrams - existing.AvailableGrams;
        if (delta != 0) AdjustMaterialStock(material, color, delta, "Stock count adjustment");
    }

    public void AdjustMaterialStock(string materialCode, string colorCode, decimal deltaGrams, string reason)
    {
        if (deltaGrams == 0) throw new ArgumentOutOfRangeException(nameof(deltaGrams), "Adjustment must be non-zero");
        var stock = FindStock(materialCode, colorCode) ?? throw new InvalidOperationException("Material stock does not exist");
        stock.Adjust(deltaGrams);
        _stockTransactions.Add(StockTransaction.Create(Id, stock.Id, deltaGrams, stock.AvailableGrams, reason));
    }

    public void ReserveMaterial(Guid jobId, string materialCode, string colorCode, decimal estimatedGrams, decimal tolerancePercent = 0.03m)
    {
        if (tolerancePercent is < 0.01m or > 0.05m) throw new ArgumentOutOfRangeException(nameof(tolerancePercent));
        if (_materialReservations.Any(r => r.JobId == jobId)) return;

        // The stock ledger is opt-in: a lab that keeps no record for this material is not
        // constrained by it (see HasStock), so there is nothing to reserve rather than an error.
        var stock = FindStock(materialCode, colorCode);
        if (stock is null) return;

        var reserved = decimal.Round(estimatedGrams * (1m + tolerancePercent), 2);
        stock.Reserve(reserved);
        _materialReservations.Add(MaterialReservation.Create(Id, stock.Id, jobId, reserved));
    }

    /// <summary>
    /// Credits material back to the lab for a print the PLATFORM caused — a hub fault
    /// (BR-RESCHED-003, decided 07/10). The consumption is still recorded by
    /// <see cref="SettleMaterial"/>, so the ledger shows both the usage and the compensation
    /// instead of hiding one of them. Labs that do not track the material are left alone, in
    /// line with the opt-in ledger.
    /// </summary>
    public void CompensateMaterial(Guid jobId, string materialCode, string colorCode, decimal grams, string reason)
    {
        if (grams <= 0) return;

        var stock = FindStock(materialCode, colorCode);
        if (stock is null) return;

        stock.Adjust(grams);
        _stockTransactions.Add(StockTransaction.Create(Id, stock.Id, grams, stock.AvailableGrams, reason, jobId));
    }

    public void ReleaseMaterial(Guid jobId)
    {
        var reservation = _materialReservations.SingleOrDefault(r => r.JobId == jobId);
        if (reservation is null) return;
        var stock = _materialStocks.Single(s => s.Id == reservation.MaterialStockId);
        stock.Release(reservation.ReservedGrams);
        _materialReservations.Remove(reservation);
    }

    public void SettleMaterial(Guid jobId, decimal actualGrams)
    {
        // The stock ledger is opt-in: ReserveMaterial created nothing when the lab keeps no
        // record for this material/colour (see HasStock), so there is nothing to settle here.
        // Settle must make the same promise as Reserve — completing a job on a lab that does
        // not track inventory is normal, not an error (it used to throw and block completion).
        var reservation = _materialReservations.SingleOrDefault(r => r.JobId == jobId);
        if (reservation is null) return;
        var stock = _materialStocks.Single(s => s.Id == reservation.MaterialStockId);
        stock.Settle(reservation.ReservedGrams, actualGrams);
        _stockTransactions.Add(StockTransaction.Create(Id, stock.Id, -actualGrams, stock.AvailableGrams, "Job completion", jobId));
        _materialReservations.Remove(reservation);
    }

    /// <summary>
    /// Whether the lab holds enough material for the job.
    ///
    /// A lab that keeps NO ledger entry for the material/colour is "not tracked", not "empty":
    /// the stock ledger is opt-in, so treating a missing record as zero would stop every lab
    /// that has never used it (which was all of them before this feature) from receiving work.
    /// A lab that does track the material is held to its balance.
    /// </summary>
    public bool HasStock(string materialCode, string colorCode, decimal requiredGrams)
    {
        var material = NormalizeStockCode(materialCode);
        var color = NormalizeStockCode(colorCode);
        var tracked = _materialStocks
            .Where(s => s.MaterialCode == material && s.ColorCode == color)
            .ToList();

        return tracked.Count == 0 || tracked.Any(s => s.AssignableGrams >= requiredGrams);
    }

    private MaterialStock? FindStock(string materialCode, string colorCode) =>
        _materialStocks.SingleOrDefault(s => s.MaterialCode == NormalizeStockCode(materialCode) && s.ColorCode == NormalizeStockCode(colorCode));

    public void UpdatePerformance(decimal onTimeDeliveryRate, decimal firstPassYield)
    {
        OnTimeDeliveryRate = Clamp(onTimeDeliveryRate);
        FirstPassYield = Clamp(firstPassYield);
    }

    private static decimal Clamp(decimal rate) => Math.Clamp(rate, 0m, 1m);

    private static string NormalizeStockCode(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Material and color are required")
            : value.Trim().ToUpperInvariant();
}
