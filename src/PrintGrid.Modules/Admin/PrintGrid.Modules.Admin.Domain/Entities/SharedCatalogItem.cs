using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Admin.Domain.Entities;

/// <summary>A revision in a shared catalogue. Revisions are retained for historical records.</summary>
public sealed class SharedCatalogItem : Entity<Guid>
{
    public string CatalogType { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public int Revision { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal? UnitRate { get; private set; }
    public bool IsPremium { get; private set; }
    public string PropertiesJson { get; private set; } = "{}";
    public bool IsDeprecated { get; private set; }
    public bool IsCurrent { get; private set; } = true;
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private SharedCatalogItem() { }

    public static SharedCatalogItem Create(
        string catalogType, string code, int revision, string name, string? description,
        decimal? unitRate, bool isPremium, string propertiesJson, bool isDeprecated,
        Guid createdBy, DateTime? createdAtUtc = null) => new()
        {
            Id = Guid.NewGuid(),
            CatalogType = catalogType.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            Revision = revision,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            UnitRate = unitRate,
            IsPremium = isPremium,
            PropertiesJson = propertiesJson,
            IsDeprecated = isDeprecated,
            CreatedBy = createdBy,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };

    public void Supersede() => IsCurrent = false;
}

public static class SharedCatalogTypes
{
    public const string Material = "Material";
    public const string Color = "Color";
    public const string Technology = "Technology";
    public const string QualityGrade = "QualityGrade";
    public const string PostProcessing = "PostProcessing";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Material, Color, Technology, QualityGrade, PostProcessing
    };
}
