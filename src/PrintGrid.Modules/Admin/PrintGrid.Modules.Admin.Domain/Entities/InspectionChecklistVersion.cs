using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Admin.Domain.Entities;

public sealed class InspectionChecklistVersion : Entity<Guid>
{
    public string QualityGradeCode { get; private set; } = string.Empty;
    public int VersionNumber { get; private set; }
    public string ItemsJson { get; private set; } = "[]";
    public bool IsActive { get; private set; } = true;
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private InspectionChecklistVersion() { }

    public static InspectionChecklistVersion Create(
        string qualityGradeCode, int versionNumber, string itemsJson,
        Guid createdBy, DateTime? createdAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(qualityGradeCode)) throw new ArgumentException("Quality grade is required.");
        if (versionNumber <= 0) throw new ArgumentOutOfRangeException(nameof(versionNumber));
        if (string.IsNullOrWhiteSpace(itemsJson)) throw new ArgumentException("Checklist items are required.");

        return new InspectionChecklistVersion
        {
            Id = Guid.NewGuid(),
            QualityGradeCode = qualityGradeCode.Trim().ToUpperInvariant(),
            VersionNumber = versionNumber,
            ItemsJson = itemsJson,
            CreatedBy = createdBy,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };
    }

    public void Supersede() => IsActive = false;
}
