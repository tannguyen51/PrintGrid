using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class DateChangeRequestConfiguration : IEntityTypeConfiguration<DateChangeRequest>
{
    public void Configure(EntityTypeBuilder<DateChangeRequest> builder)
    {
        builder.ToTable("date_change_requests", "scheduling");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
        builder.Property(r => r.Token).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(r => r.Token).IsUnique();
        builder.HasIndex(r => new { r.JobId, r.CreatedAtUtc });

        builder.Ignore(r => r.DomainEvents);
    }
}
