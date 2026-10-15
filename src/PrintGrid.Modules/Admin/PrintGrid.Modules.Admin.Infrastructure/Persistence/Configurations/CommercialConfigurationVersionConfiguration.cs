using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.Modules.Admin.Infrastructure.Persistence.Configurations;

public sealed class CommercialConfigurationVersionConfiguration : IEntityTypeConfiguration<CommercialConfigurationVersion>
{
    public void Configure(EntityTypeBuilder<CommercialConfigurationVersion> builder)
    {
        builder.ToTable("commercial_configuration_versions", "admin");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ParametersJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ChangeSummary).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(16).IsRequired();
        builder.HasIndex(x => x.VersionNumber).IsUnique();
        builder.HasIndex(x => x.Status).IsUnique().HasFilter("\"Status\" = 'Active'");
    }
}
