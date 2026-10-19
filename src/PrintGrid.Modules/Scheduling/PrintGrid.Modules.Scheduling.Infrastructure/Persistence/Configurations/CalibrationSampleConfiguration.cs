using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class CalibrationSampleConfiguration : IEntityTypeConfiguration<CalibrationSample>
{
    public void Configure(EntityTypeBuilder<CalibrationSample> builder)
    {
        builder.ToTable("calibration_samples", "scheduling");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.MaterialCode).HasMaxLength(32).IsRequired();
        builder.Property(s => s.EstimatedMinutes).HasPrecision(10, 2).IsRequired();
        builder.Property(s => s.ActualMinutes).HasPrecision(10, 2).IsRequired();
        builder.Property(s => s.CreatedAtUtc).IsRequired();

        builder.Ignore(s => s.Ratio);

        // A completed job is sampled at most once, no matter how often the nightly job runs.
        builder.HasIndex(s => s.JobId).IsUnique();
        builder.HasIndex(s => new { s.MachineId, s.CreatedAtUtc });

        builder.HasOne<Machine>()
            .WithMany()
            .HasForeignKey(s => s.MachineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
