using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.Modules.Admin.Infrastructure.Persistence.Configurations;

public sealed class DefectTaxonomyItemConfiguration : IEntityTypeConfiguration<DefectTaxonomyItem>
{
    public void Configure(EntityTypeBuilder<DefectTaxonomyItem> builder)
    {
        builder.ToTable("defect_taxonomy_items", "admin");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.DefaultFaultAttribution).HasMaxLength(16);
        builder.HasAlternateKey(x => x.Code);
        builder.HasIndex(x => new { x.IsDeprecated, x.Name });
    }
}
