using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class LabConfiguration : IEntityTypeConfiguration<Lab>
{
    public void Configure(EntityTypeBuilder<Lab> builder)
    {
        builder.ToTable("labs", "scheduling");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name).HasMaxLength(200).IsRequired();
        builder.Property(l => l.City).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(l => l.OperatingHours).HasMaxLength(120).IsRequired();
        builder.Property(l => l.ContactPhone).HasMaxLength(32);
        builder.Property(l => l.Address).HasMaxLength(300);
        builder.Property(l => l.RejectionReason).HasMaxLength(500);
        builder.Property(l => l.OnTimeDeliveryRate).HasPrecision(5, 4).IsRequired();
        builder.Property(l => l.FirstPassYield).HasPrecision(5, 4).IsRequired();

        builder.HasIndex(l => l.IsActive);
        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.OwnerUserId);
        builder.Ignore(l => l.IsDeclarationComplete);

        builder.HasMany(l => l.Machines)
            .WithOne()
            .HasForeignKey(m => m.LabId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Machines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(l => l.MaterialStocks)
            .WithOne()
            .HasForeignKey(s => s.LabId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(l => l.MaterialStocks).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(l => l.StockTransactions).WithOne().HasForeignKey(t => t.LabId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(l => l.StockTransactions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(l => l.MaterialReservations).WithOne().HasForeignKey(r => r.LabId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(l => l.MaterialReservations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(l => l.DomainEvents);
    }
}
