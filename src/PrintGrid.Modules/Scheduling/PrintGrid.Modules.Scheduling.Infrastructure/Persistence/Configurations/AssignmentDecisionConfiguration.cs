using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

/// <summary>
/// Append-only decision log (FR-SCHED-009 / BR-ASSIGN-004). Candidate sets and score
/// breakdowns are stored as jsonb — the log must reproduce a decision exactly as it was
/// made, so the shape is deliberately not normalised into per-criterion rows.
/// </summary>
public class AssignmentDecisionConfiguration : IEntityTypeConfiguration<AssignmentDecision>
{
    public void Configure(EntityTypeBuilder<AssignmentDecision> builder)
    {
        builder.ToTable("assignment_decisions", "scheduling");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Trigger).HasMaxLength(64).IsRequired();
        builder.Property(d => d.ActorType).HasMaxLength(16).IsRequired();
        builder.Property(d => d.ActorId).HasMaxLength(128);
        builder.Property(d => d.Outcome).HasMaxLength(32).IsRequired();
        builder.Property(d => d.Reason).HasMaxLength(500);
        builder.Property(d => d.ScoringConfigVersion).HasMaxLength(16).IsRequired();

        builder.Property(d => d.CandidatesJson).HasColumnType("jsonb").IsRequired();
        builder.Property(d => d.RankingJson).HasColumnType("jsonb").IsRequired();
        builder.Property(d => d.ScoringConfigJson).HasColumnType("jsonb").IsRequired();

        builder.Property(d => d.ChosenScore).HasPrecision(9, 4);

        builder.HasIndex(d => new { d.JobId, d.CreatedAtUtc });
        builder.HasIndex(d => d.OrderItemId);
    }
}
