using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class MaterialStockConfiguration : IEntityTypeConfiguration<MaterialStock>
{
    public void Configure(EntityTypeBuilder<MaterialStock> builder)
    {
        builder.ToTable("material_stocks", "scheduling");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.MaterialCode).HasMaxLength(32).IsRequired();
        builder.Property(s => s.ColorCode).HasMaxLength(32).IsRequired();
        builder.Property(s => s.AvailableGrams).HasPrecision(12, 2).IsRequired();
        builder.Property(s => s.ReservedGrams).HasPrecision(12, 2).IsRequired();
        builder.Property(s => s.ReorderPointGrams).HasPrecision(12, 2).IsRequired();
        builder.Ignore(s => s.AssignableGrams);
        builder.Ignore(s => s.IsLowStock);
        builder.HasIndex(s => new { s.LabId, s.MaterialCode, s.ColorCode }).IsUnique();
    }
}
