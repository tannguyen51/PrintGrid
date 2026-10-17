using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public sealed class ShipmentBatchConfiguration : IEntityTypeConfiguration<ShipmentBatch>
{
    public void Configure(EntityTypeBuilder<ShipmentBatch> builder)
    {
        builder.ToTable("shipment_batches", "scheduling");
        builder.HasKey(batch => batch.Id);
        builder.Property(batch => batch.TransportZone).HasMaxLength(32).IsRequired();
        builder.Property(batch => batch.WeightClass).HasMaxLength(16).IsRequired();
        builder.Property(batch => batch.TotalWeightGrams).HasPrecision(12, 2).IsRequired();
        builder.Property(batch => batch.ZoneRate).HasPrecision(14, 2).IsRequired();
        builder.Property(batch => batch.WeightMultiplier).HasPrecision(8, 3).IsRequired();
        builder.Property(batch => batch.TransportAllowance).HasPrecision(14, 2).IsRequired();
        builder.Property(batch => batch.JobCount).IsRequired();
        builder.Property(batch => batch.Currency).HasMaxLength(3).IsRequired();
        builder.Property(batch => batch.WaybillNumber).HasMaxLength(64).IsRequired();
        builder.Property(batch => batch.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.HasIndex(batch => batch.WaybillNumber).IsUnique();
        builder.HasIndex(batch => new { batch.LabId, batch.ScheduledTransferDate });
        builder.Ignore(batch => batch.DomainEvents);
    }
}
