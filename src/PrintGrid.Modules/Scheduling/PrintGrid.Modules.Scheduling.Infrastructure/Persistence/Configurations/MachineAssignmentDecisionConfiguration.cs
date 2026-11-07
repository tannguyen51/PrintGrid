using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class MachineAssignmentDecisionConfiguration : IEntityTypeConfiguration<MachineAssignmentDecision>
{
    public void Configure(EntityTypeBuilder<MachineAssignmentDecision> builder)
    {
        builder.ToTable("machine_assignment_decisions", "scheduling");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.JobId).IsRequired();
        builder.Property(d => d.MachineId).IsRequired();
        builder.Property(d => d.Decision).HasMaxLength(16).IsRequired();
        builder.Property(d => d.RejectionReason).HasMaxLength(500);
        builder.Property(d => d.CreatedAtUtc).IsRequired();

        builder.HasIndex(d => new { d.JobId, d.MachineId });
    }
}
