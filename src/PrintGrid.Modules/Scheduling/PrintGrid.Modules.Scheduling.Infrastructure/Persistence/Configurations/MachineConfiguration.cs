using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> builder)
    {
        builder.ToTable("machines", "scheduling");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Model).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Technology).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(m => m.MinLayerHeightMm).HasPrecision(4, 3).IsRequired();
        builder.Property(m => m.AchievableToleranceMm).HasPrecision(5, 3).IsRequired();
        builder.Property(m => m.SpeedFactor).HasPrecision(5, 3).IsRequired();
        builder.Property(m => m.CalibrationFactor).HasPrecision(5, 3).IsRequired();
        builder.Property(m => m.CalibrationSampleCount).IsRequired();
        builder.Property(m => m.CalibrationMape).HasPrecision(6, 4).IsRequired();
        builder.Property(m => m.MaintenanceReason).HasMaxLength(300);
        builder.Property(m => m.ProfileStatus).HasMaxLength(30).IsRequired();
        builder.Property(m => m.ProfileReviewReason).HasMaxLength(1000);
        builder.Property(m => m.AllowsCustomerPinning).IsRequired();
        builder.Property(m => m.CuraProfileIds).HasColumnType("uuid[]").IsRequired();
        builder.OwnsOne(m => m.PrinterConfiguration, c =>
        {
            c.Property(p => p.Manufacturer).HasMaxLength(100);
            c.Property(p => p.NozzleDiameterMm).HasPrecision(6, 3);
            c.Property(p => p.FilamentDiameterMm).HasPrecision(6, 3);
            c.Property(p => p.MaxNozzleTemperatureC).HasPrecision(6, 2);
            c.Property(p => p.MaxBedTemperatureC).HasPrecision(6, 2);
            c.Property(p => p.PricePerHour).HasPrecision(18, 2);
            c.Property(p => p.FailureRate).HasPrecision(7, 6);
        });

        builder.HasIndex(m => new { m.LabId, m.Status });

        builder.OwnsOne(m => m.BuildVolume, vol =>
        {
            vol.Property(v => v.WidthMm).HasColumnName("build_width_mm").HasPrecision(8, 2).IsRequired();
            vol.Property(v => v.DepthMm).HasColumnName("build_depth_mm").HasPrecision(8, 2).IsRequired();
            vol.Property(v => v.HeightMm).HasColumnName("build_height_mm").HasPrecision(8, 2).IsRequired();
        });

        builder.Property<List<string>>("_supportedMaterials")
            .HasColumnName("supported_materials")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Ignore(m => m.SupportedMaterials);
    }
}
