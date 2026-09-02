using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class Machine : Entity<Guid>
{
    private readonly List<string> _supportedMaterials = new();

    public Guid LabId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public PrintTechnology Technology { get; private set; }
    public BuildVolume BuildVolume { get; private set; } = null!;
    public decimal MinLayerHeightMm { get; private set; }
    public decimal AchievableToleranceMm { get; private set; }
    public MachineStatus Status { get; private set; }
    public decimal SpeedFactor { get; private set; } = 1.0m;

    public IReadOnlyCollection<string> SupportedMaterials => _supportedMaterials.AsReadOnly();

    private Machine() { }

    public static Machine Register(
        Guid labId,
        string name,
        string model,
        PrintTechnology technology,
        BuildVolume buildVolume,
        decimal minLayerHeightMm,
        decimal achievableToleranceMm,
        IEnumerable<string> supportedMaterials)
    {
        var machine = new Machine
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            Name = name.Trim(),
            Model = model.Trim(),
            Technology = technology,
            BuildVolume = buildVolume,
            MinLayerHeightMm = minLayerHeightMm,
            AchievableToleranceMm = achievableToleranceMm,
            Status = MachineStatus.Idle
        };

        machine._supportedMaterials.AddRange(
            supportedMaterials.Select(m => m.Trim().ToUpperInvariant()).Distinct());

        return machine;
    }

    public bool Supports(JobSpecification spec) =>
        Technology == spec.Technology
        && BuildVolume.CanFit(spec.RequiredVolume)
        && MinLayerHeightMm <= spec.LayerHeightMm
        && AchievableToleranceMm <= spec.ToleranceMm
        && _supportedMaterials.Contains(spec.MaterialCode);

    public void SetStatus(MachineStatus status) => Status = status;

    public void AdjustSpeedFactor(decimal factor)
    {
        if (factor is < 0.5m or > 2.0m)
            throw new ArgumentOutOfRangeException(nameof(factor), "Speed factor must stay between 0.5 and 2.0");

        SpeedFactor = decimal.Round(factor, 3);
    }
}
