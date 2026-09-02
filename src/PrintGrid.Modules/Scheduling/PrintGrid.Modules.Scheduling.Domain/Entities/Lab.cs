using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class Lab : AggregateRoot<Guid>
{
    private readonly List<Machine> _machines = new();

    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public decimal OnTimeDeliveryRate { get; private set; } = 1.0m;
    public decimal FirstPassYield { get; private set; } = 1.0m;
    public int TransitDaysToHub { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<Machine> Machines => _machines.AsReadOnly();

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

    public void UpdatePerformance(decimal onTimeDeliveryRate, decimal firstPassYield)
    {
        OnTimeDeliveryRate = Clamp(onTimeDeliveryRate);
        FirstPassYield = Clamp(firstPassYield);
    }

    private static decimal Clamp(decimal rate) => Math.Clamp(rate, 0m, 1m);
}
