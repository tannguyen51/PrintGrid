using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class OpsEscalationConfiguration : IEntityTypeConfiguration<OpsEscalation>
{
    public void Configure(EntityTypeBuilder<OpsEscalation> builder)
    {
        builder.ToTable("ops_escalations", "scheduling");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(500).IsRequired();

        builder.HasIndex(e => new { e.Status, e.CreatedAtUtc });
        builder.HasIndex(e => e.JobId);

        builder.Ignore(e => e.DomainEvents);
    }
}
