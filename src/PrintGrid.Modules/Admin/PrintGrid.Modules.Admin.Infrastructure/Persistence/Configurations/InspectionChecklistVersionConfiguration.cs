using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.Modules.Admin.Infrastructure.Persistence.Configurations;

public sealed class InspectionChecklistVersionConfiguration : IEntityTypeConfiguration<InspectionChecklistVersion>
{
    public void Configure(EntityTypeBuilder<InspectionChecklistVersion> builder)
    {
        builder.ToTable("inspection_checklist_versions", "admin");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QualityGradeCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ItemsJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.QualityGradeCode, x.VersionNumber }).IsUnique();
        builder.HasIndex(x => new { x.QualityGradeCode, x.IsActive })
            .IsUnique().HasFilter("\"IsActive\" = TRUE");
    }
}
