using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;
namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;
public class DateChangeRequestConfiguration : IEntityTypeConfiguration<DateChangeRequest>
{
    public void Configure(EntityTypeBuilder<DateChangeRequest> b)
    {
        b.ToTable("date_change_requests", "scheduling"); b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired(); b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.HasIndex(x => new { x.JobId, x.Status });
    }
}
