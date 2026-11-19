using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Admin.Domain.Entities;

/// <summary>Immutable commercial parameters; activation is forward-only (BR-CONFIG-001/003).</summary>
public sealed class CommercialConfigurationVersion : Entity<Guid>
{
    public int VersionNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ParametersJson { get; private set; } = "{}";
    public string ChangeSummary { get; private set; } = string.Empty;
    public string Status { get; private set; } = ConfigurationStatuses.Draft;
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ActivatedAtUtc { get; private set; }

    /// <summary>
    /// Optional future activation time. When set, the version remains Draft until that moment,
    /// then becomes Active. This separates "already activated" from "scheduled to activate".
    /// </summary>
    public DateTime? ScheduledActivationAtUtc { get; private set; }

    private CommercialConfigurationVersion() { }

    public static CommercialConfigurationVersion Create(
        int versionNumber, string name, string parametersJson, string changeSummary,
        Guid createdBy, DateTime? createdAtUtc = null) => new()
    {
        Id = Guid.NewGuid(),
        VersionNumber = versionNumber,
        Name = name.Trim(),
        ParametersJson = parametersJson,
        ChangeSummary = changeSummary.Trim(),
        CreatedBy = createdBy,
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
    };

    /// <summary>Activate immediately.</summary>
    public void Activate(DateTime? activatedAtUtc = null)
    {
        if (Status != ConfigurationStatuses.Draft)
            throw new InvalidOperationException("Only a draft configuration can be activated.");
        Status = ConfigurationStatuses.Active;
        ActivatedAtUtc = activatedAtUtc ?? DateTime.UtcNow;
        ScheduledActivationAtUtc = null;
    }

    /// <summary>Schedule activation at a future time; remains Draft until then.</summary>
    public void ScheduleActivation(DateTime scheduledActivationAtUtc)
    {
        if (Status != ConfigurationStatuses.Draft)
            throw new InvalidOperationException("Only a draft configuration can be scheduled.");
        if (scheduledActivationAtUtc <= DateTime.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(scheduledActivationAtUtc), "Scheduled activation must be in the future.");
        ScheduledActivationAtUtc = scheduledActivationAtUtc;
    }

    /// <summary>
    /// Materialize a scheduled activation once its time has come.
    /// Idempotent: if already Active, does nothing.
    /// </summary>
    public void MaterializeScheduledActivation(DateTime? asOfUtc = null)
    {
        if (Status != ConfigurationStatuses.Draft || !ScheduledActivationAtUtc.HasValue)
            return;
        if ((asOfUtc ?? DateTime.UtcNow) < ScheduledActivationAtUtc.Value)
            return;
        Status = ConfigurationStatuses.Active;
        ActivatedAtUtc = ScheduledActivationAtUtc.Value;
        ScheduledActivationAtUtc = null;
    }

    public void Archive()
    {
        if (Status == ConfigurationStatuses.Active)
            Status = ConfigurationStatuses.Archived;
    }
}

public static class ConfigurationStatuses
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Archived = "Archived";
}
