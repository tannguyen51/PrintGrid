using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs", "scheduling");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(j => j.FailureReason).HasMaxLength(500);
        builder.Property(j => j.EstimatedPrintMinutes).IsRequired();

        builder.HasIndex(j => new { j.Status, j.MachineId });
        builder.HasIndex(j => j.InternalDueDate);
        builder.HasIndex(j => j.OrderItemId);

        builder.OwnsOne(j => j.Specification, spec =>
        {
            spec.Property(s => s.MaterialCode).HasColumnName("material_code").HasMaxLength(32).IsRequired();
            spec.Property(s => s.ColorCode).HasColumnName("color_code").HasMaxLength(32).IsRequired();
            spec.Property(s => s.LayerHeightMm).HasColumnName("layer_height_mm").HasPrecision(4, 3).IsRequired();
            spec.Property(s => s.ToleranceMm).HasColumnName("tolerance_mm").HasPrecision(5, 3).IsRequired();
            spec.Property(s => s.Technology).HasColumnName("technology").HasConversion<string>().HasMaxLength(16).IsRequired();
            spec.Property(s => s.MaterialGrams).HasColumnName("material_grams").HasPrecision(10, 2).IsRequired();

            spec.OwnsOne(s => s.RequiredVolume, vol =>
            {
                vol.Property(v => v.WidthMm).HasColumnName("required_width_mm").HasPrecision(8, 2).IsRequired();
                vol.Property(v => v.DepthMm).HasColumnName("required_depth_mm").HasPrecision(8, 2).IsRequired();
                vol.Property(v => v.HeightMm).HasColumnName("required_height_mm").HasPrecision(8, 2).IsRequired();
            });
        });

        builder.Ignore(j => j.DomainEvents);
    }
}
