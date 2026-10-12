using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.Modules.Admin.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "admin");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorDisplay).HasMaxLength(254).IsRequired();
        builder.Property(x => x.ActorRole).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActionClass).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(128);
        builder.Property(x => x.OldValuesJson).HasColumnType("jsonb");
        builder.Property(x => x.NewValuesJson).HasColumnType("jsonb");
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.TraceId).HasMaxLength(128);
        builder.Property(x => x.RequirementCodesJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => new { x.ActionClass, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.ActorId, x.CreatedAtUtc });
    }
}
