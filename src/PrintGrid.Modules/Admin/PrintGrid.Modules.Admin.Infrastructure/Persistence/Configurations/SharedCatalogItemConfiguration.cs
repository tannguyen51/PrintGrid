using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.Modules.Admin.Infrastructure.Persistence.Configurations;

public sealed class SharedCatalogItemConfiguration : IEntityTypeConfiguration<SharedCatalogItem>
{
    public void Configure(EntityTypeBuilder<SharedCatalogItem> builder)
    {
        builder.ToTable("shared_catalog_items", "admin");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CatalogType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.UnitRate).HasPrecision(18, 2);
        builder.Property(x => x.PropertiesJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.CatalogType, x.Code, x.Revision }).IsUnique();
        builder.HasIndex(x => new { x.CatalogType, x.Code, x.IsCurrent })
            .IsUnique().HasFilter("\"IsCurrent\" = TRUE");
        builder.HasIndex(x => new { x.CatalogType, x.IsCurrent, x.IsDeprecated });
    }
}
