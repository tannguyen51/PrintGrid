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
    public DateTime? MaintenanceFromUtc { get; private set; }
    public DateTime? MaintenanceUntilUtc { get; private set; }
    public string? MaintenanceReason { get; private set; }

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
            supportedMaterials.Select(m => m.Trim().ToUpperInvariant()).Where(m => m.Length > 0).Distinct());

        return machine;
    }

    public void UpdateSpecifications(
        string name,
        string model,
        PrintTechnology technology,
        BuildVolume buildVolume,
        decimal minLayerHeightMm,
        decimal achievableToleranceMm,
        IEnumerable<string> supportedMaterials)
    {
        Name = name.Trim();
        Model = model.Trim();
        Technology = technology;
        BuildVolume = buildVolume;
        MinLayerHeightMm = minLayerHeightMm;
        AchievableToleranceMm = achievableToleranceMm;

        _supportedMaterials.Clear();
        _supportedMaterials.AddRange(
            supportedMaterials.Select(m => m.Trim().ToUpperInvariant()).Where(m => m.Length > 0).Distinct());
    }

    public void ScheduleMaintenance(DateTime? fromUtc, DateTime? untilUtc, string? reason, bool setMaintenanceNow = false)
    {
        if (fromUtc.HasValue && untilUtc.HasValue && untilUtc.Value <= fromUtc.Value)
            throw new ArgumentException("Maintenance end time must be after start time");

        MaintenanceFromUtc = fromUtc;
        MaintenanceUntilUtc = untilUtc;
        MaintenanceReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        var now = DateTime.UtcNow;
        if (setMaintenanceNow || (fromUtc.HasValue && untilUtc.HasValue && now >= fromUtc.Value && now < untilUtc.Value))
        {
            Status = MachineStatus.Maintenance;
        }
    }

    public void ClearMaintenance()
    {
        MaintenanceFromUtc = null;
        MaintenanceUntilUtc = null;
        MaintenanceReason = null;
        if (Status == MachineStatus.Maintenance)
            Status = MachineStatus.Idle;
    }

    public bool IsInMaintenanceAt(DateTime utcTime)
    {
        if (Status == MachineStatus.Maintenance) return true;
        if (MaintenanceFromUtc.HasValue && MaintenanceUntilUtc.HasValue)
            return utcTime >= MaintenanceFromUtc.Value && utcTime < MaintenanceUntilUtc.Value;
        if (!MaintenanceFromUtc.HasValue && MaintenanceUntilUtc.HasValue)
            return utcTime < MaintenanceUntilUtc.Value;
        return false;
    }

    public bool Supports(JobSpecification spec) =>
        Technology == spec.Technology
        && BuildVolume.CanFit(spec.RequiredVolume)
        && MinLayerHeightMm <= spec.LayerHeightMm
        && AchievableToleranceMm <= spec.ToleranceMm
        && _supportedMaterials.Contains(spec.MaterialCode);

    public void SetStatus(MachineStatus status)
    {
        Status = status;
        if (status == MachineStatus.Idle)
        {
            MaintenanceFromUtc = null;
            MaintenanceUntilUtc = null;
            MaintenanceReason = null;
        }
    }

    public void AdjustSpeedFactor(decimal factor)
    {
        if (factor is < 0.5m or > 2.0m)
            throw new ArgumentOutOfRangeException(nameof(factor), "Speed factor must stay between 0.5 and 2.0");

        SpeedFactor = decimal.Round(factor, 3);
    }
}
