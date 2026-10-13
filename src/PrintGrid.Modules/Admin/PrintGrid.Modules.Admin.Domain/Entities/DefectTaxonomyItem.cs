using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Admin.Domain.Entities;

public sealed class DefectTaxonomyItem : Entity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? DefaultFaultAttribution { get; private set; }
    public bool IsDeprecated { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private DefectTaxonomyItem() { }

    public static DefectTaxonomyItem Create(
        string code, string name, string? description, string? defaultFaultAttribution,
        bool isDeprecated, Guid actorId, DateTime? createdAtUtc = null)
    {
        var item = new DefectTaxonomyItem
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            CreatedBy = actorId,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };
        item.Update(name, description, defaultFaultAttribution, isDeprecated, actorId, createdAtUtc);
        item.UpdatedBy = null;
        item.UpdatedAtUtc = null;
        return item;
    }

    public void Update(string name, string? description, string? defaultFaultAttribution,
        bool isDeprecated, Guid actorId, DateTime? updatedAtUtc = null)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DefaultFaultAttribution = string.IsNullOrWhiteSpace(defaultFaultAttribution)
            ? null : defaultFaultAttribution.Trim();
        IsDeprecated = isDeprecated;
        UpdatedBy = actorId;
        UpdatedAtUtc = updatedAtUtc ?? DateTime.UtcNow;
    }
}
