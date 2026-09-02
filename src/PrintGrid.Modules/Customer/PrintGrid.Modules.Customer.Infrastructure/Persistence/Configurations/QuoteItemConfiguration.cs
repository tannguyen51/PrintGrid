using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Configurations;

public class QuoteItemConfiguration : IEntityTypeConfiguration<QuoteItem>
{
    public void Configure(EntityTypeBuilder<QuoteItem> builder)
    {
        builder.ToTable("quote_items", "customer");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).IsRequired();
        builder.Property(i => i.EstimatedPrintMinutes).IsRequired();
        builder.Property(i => i.EstimatedMaterialGrams).HasPrecision(10, 2).IsRequired();
        builder.HasIndex(i => i.QuoteId);

        builder.OwnsOne(i => i.UnitPrice, price =>
        {
            price.Property(p => p.Amount).HasColumnName("unit_amount").HasPrecision(18, 2).IsRequired();
            price.Property(p => p.Currency).HasColumnName("unit_currency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(i => i.Configuration, config =>
        {
            config.Property(c => c.MaterialCode).HasColumnName("material_code").HasMaxLength(32).IsRequired();
            config.Property(c => c.ColorCode).HasColumnName("color_code").HasMaxLength(32).IsRequired();
            config.Property(c => c.LayerHeightMm).HasColumnName("layer_height_mm").HasPrecision(4, 3).IsRequired();
            config.Property(c => c.InfillPercent).HasColumnName("infill_percent").IsRequired();
            config.Property(c => c.ToleranceMm).HasColumnName("tolerance_mm").HasPrecision(5, 3).IsRequired();
        });
    }
}
