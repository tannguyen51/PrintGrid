using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.Modules.Admin.Infrastructure.Persistence.Configurations;

public sealed class SampleModelConfiguration : IEntityTypeConfiguration<SampleModel>
{
    public void Configure(EntityTypeBuilder<SampleModel> builder)
    {
        builder.ToTable("sample_models", "admin");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Category).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.FileFormat).HasMaxLength(16).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64);
        builder.Property(x => x.CoverImageUrl).HasMaxLength(1000);

        builder.Property(x => x.BoundingWidthMm).HasPrecision(18, 2);
        builder.Property(x => x.BoundingDepthMm).HasPrecision(18, 2);
        builder.Property(x => x.BoundingHeightMm).HasPrecision(18, 2);
        builder.Property(x => x.VolumeCm3).HasPrecision(18, 3);
        builder.Property(x => x.EstimatedMaterialGrams).HasPrecision(18, 2);

        // PostgreSQL text[] — same mapping as customer.models.Tags.
        builder.PrimitiveCollection(x => x.Tags);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.IsPublished, x.SortOrder });
    }
}
