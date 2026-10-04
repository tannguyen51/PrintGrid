using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class Lab : AggregateRoot<Guid>
{
    private readonly List<Machine> _machines = new();
    private readonly List<MaterialStock> _materialStocks = new();

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

    public void SetMaterialStock(string materialCode, string colorCode, decimal availableGrams)
    {
        var material = NormalizeStockCode(materialCode);
        var color = NormalizeStockCode(colorCode);
        var existing = _materialStocks.SingleOrDefault(s => s.MaterialCode == material && s.ColorCode == color);
        if (existing is null)
            _materialStocks.Add(MaterialStock.Create(Id, material, color, availableGrams));
        else
            existing.SetAvailable(availableGrams);
    }

    public bool HasStock(string materialCode, string colorCode, decimal requiredGrams) =>
        _materialStocks.Any(s =>
            s.MaterialCode == NormalizeStockCode(materialCode)
            && s.ColorCode == NormalizeStockCode(colorCode)
            && s.AvailableGrams >= requiredGrams);

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
