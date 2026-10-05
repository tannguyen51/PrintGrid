using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("stock_transactions", "scheduling");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TransactionCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DeltaGrams).HasPrecision(12, 2).IsRequired();
        builder.Property(x => x.RunningTotalGrams).HasPrecision(12, 2).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(250).IsRequired();
        builder.HasIndex(x => x.TransactionCode).IsUnique();
        builder.HasIndex(x => new { x.LabId, x.CreatedAtUtc });
    }
}
