using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class LabStaffConfiguration : IEntityTypeConfiguration<LabStaff>
{
    public void Configure(EntityTypeBuilder<LabStaff> builder)
    {
        builder.ToTable("lab_staff", "scheduling");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LabId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // One membership row per (lab, account); an account belongs to one lab in the MVP.
        builder.HasIndex(x => new { x.LabId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
