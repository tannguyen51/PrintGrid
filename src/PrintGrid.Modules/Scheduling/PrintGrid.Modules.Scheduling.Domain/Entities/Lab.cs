using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class Lab : AggregateRoot<Guid>
{
    private readonly List<Machine> _machines = new();
    private readonly List<MaterialStock> _materialStocks = new();
    private readonly List<StockTransaction> _stockTransactions = new();
    private readonly List<MaterialReservation> _materialReservations = new();

    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public Guid? OwnerUserId { get; private set; }
    public bool IsActive { get; private set; }
    public LabStatus Status { get; private set; } = LabStatus.Active;
    public string OperatingHours { get; private set; } = "T2-T7 08:00-18:00";
    public Guid? OwnerUserId { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? Address { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public decimal OnTimeDeliveryRate { get; private set; } = 1.0m;
    public decimal FirstPassYield { get; private set; } = 1.0m;
    public int TransitDaysToHub { get; private set; }
    public string WorkingDays { get; private set; } = "Monday,Tuesday,Wednesday,Thursday,Friday";
    public string OpeningTime { get; private set; } = "08:00";
    public string ClosingTime { get; private set; } = "17:00";
    public bool IsInGoodStanding { get; private set; } = true;
    public int? MaxConcurrentJobs { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsDeclarationComplete =>
        !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(City)
        && !string.IsNullOrWhiteSpace(OperatingHours)
        && TransitDaysToHub >= 0
        && _machines.Count > 0;

    public IReadOnlyCollection<Machine> Machines => _machines.AsReadOnly();
    public IReadOnlyCollection<MaterialStock> MaterialStocks => _materialStocks.AsReadOnly();
    public IReadOnlyCollection<StockTransaction> StockTransactions => _stockTransactions.AsReadOnly();
    public IReadOnlyCollection<MaterialReservation> MaterialReservations => _materialReservations.AsReadOnly();

    private Lab() { }

    public static Lab Onboard(
        string name,
        string city,
        int transitDaysToHub,
        Guid? ownerUserId = null,
        string workingDays = "Monday,Tuesday,Wednesday,Thursday,Friday",
        string openingTime = "08:00",
        string closingTime = "17:00",
        string? operatingHours = null)
    {
        if (transitDaysToHub < 0 || (ownerUserId.HasValue && transitDaysToHub == 0))
            throw new ArgumentOutOfRangeException(nameof(transitDaysToHub), "Transit days must be positive");
        if (string.IsNullOrWhiteSpace(workingDays))
            throw new ArgumentException("At least one working day is required", nameof(workingDays));
        if (!TimeOnly.TryParse(openingTime, out var opening) || !TimeOnly.TryParse(closingTime, out var closing) || closing <= opening)
            throw new ArgumentException("Working hours are invalid");

        return new Lab
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            City = city.Trim(),
            OwnerUserId = ownerUserId,
            TransitDaysToHub = transitDaysToHub,
            OperatingHours = string.IsNullOrWhiteSpace(operatingHours) ? $"{workingDays} {opening:HH:mm}-{closing:HH:mm}".Trim() : operatingHours.Trim(),
            WorkingDays = workingDays.Trim(),
            OpeningTime = opening.ToString("HH:mm"),
            ClosingTime = closing.ToString("HH:mm"),
            IsActive = ownerUserId is null,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Lab self-registration (FR-LAB-001, BR-OPS-005/006): starts in <see cref="LabStatus.Pending"/>
    /// with <see cref="IsActive"/> = false so the lab receives no jobs until approved by Admin.
    /// </summary>
    public static Lab SelfRegister(
        string name,
        string city,
        int transitDaysToHub,
        string operatingHours,
        Guid? ownerUserId = null,
        string? contactPhone = null,
        string? address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Lab name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required", nameof(city));
        if (string.IsNullOrWhiteSpace(operatingHours))
            throw new ArgumentException("Operating hours are required", nameof(operatingHours));
        if (transitDaysToHub < 0)
            throw new ArgumentOutOfRangeException(nameof(transitDaysToHub), "Transit days cannot be negative");

        return new Lab
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            City = city.Trim(),
            TransitDaysToHub = transitDaysToHub,
            OperatingHours = operatingHours.Trim(),
            OwnerUserId = ownerUserId,
            ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            Status = LabStatus.Pending,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(
        string name,
        string city,
        int transitDaysToHub,
        string operatingHours,
        string? contactPhone = null,
        string? address = null)
    {
        if (transitDaysToHub < 0)
            throw new ArgumentOutOfRangeException(nameof(transitDaysToHub), "Transit days cannot be negative");

        Name = name.Trim();
        City = city.Trim();
        TransitDaysToHub = transitDaysToHub;
        OperatingHours = operatingHours.Trim();
        ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();

        if (Status == LabStatus.Rejected)
        {
            Status = LabStatus.Pending;
            RejectionReason = null;
        }
    }

    public void ClaimOwnership(Guid ownerUserId)
    {
        if (OwnerUserId is null)
            OwnerUserId = ownerUserId;
    }

    public Result Approve(Guid? approvedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(City) || string.IsNullOrWhiteSpace(OperatingHours) || TransitDaysToHub < 0)
            return Result.Failure(Error.Validation("Hồ sơ xưởng chưa đầy đủ (thiếu tên, thành phố, lịch làm việc hoặc thời gian chuyển về Hub)."));

        if (_machines.Count == 0)
            return Result.Failure(Error.Conflict("Không thể duyệt xưởng khi chưa khai báo ít nhất 1 máy in."));

        Status = LabStatus.Active;
        IsActive = true;
        RejectionReason = null;
        ApprovedAtUtc = DateTime.UtcNow;
        ApprovedByUserId = approvedByUserId;
        return Result.Success();
    }

    public Result Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("Lý do từ chối không được để trống."));

        Status = LabStatus.Rejected;
        IsActive = false;
        RejectionReason = reason.Trim();
        return Result.Success();
    }

    public void AddMachine(Machine machine) => _machines.Add(machine);

    public bool RemoveMachine(Guid machineId)
    {
        var machine = _machines.SingleOrDefault(x => x.Id == machineId);
        return machine is not null && _machines.Remove(machine);
    }

    public void Deactivate()
    {
        IsActive = false;
        Status = LabStatus.Inactive;
    }

    public void Activate()
    {
        IsActive = true;
        Status = LabStatus.Active;
    }

    public void SuspendFromAssignments() => IsInGoodStanding = false;

    public void RestoreGoodStanding() => IsInGoodStanding = true;

    public void SetCapacityLimit(int? maxConcurrentJobs)
    {
        if (maxConcurrentJobs is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrentJobs), "Capacity limit must be positive");

        MaxConcurrentJobs = maxConcurrentJobs;
    }

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
