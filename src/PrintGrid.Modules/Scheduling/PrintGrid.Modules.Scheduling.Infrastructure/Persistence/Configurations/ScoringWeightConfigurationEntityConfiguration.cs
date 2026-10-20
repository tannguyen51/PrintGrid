using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public sealed class ScoringWeightConfigurationEntityConfiguration : IEntityTypeConfiguration<ScoringWeightConfiguration>
{
    public void Configure(EntityTypeBuilder<ScoringWeightConfiguration> builder)
    {
        builder.ToTable("scoring_weight_configurations", "scheduling");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DueDateSlack).HasPrecision(8, 6);
        builder.Property(x => x.LabReliability).HasPrecision(8, 6);
        builder.Property(x => x.LoadBalance).HasPrecision(8, 6);
        builder.Property(x => x.Cost).HasPrecision(8, 6);
        builder.Property(x => x.Transit).HasPrecision(8, 6);
        builder.HasIndex(x => x.Version).IsUnique();
        builder.HasIndex(x => x.IsActive).HasFilter("\"IsActive\" = TRUE").IsUnique();
    }
}
