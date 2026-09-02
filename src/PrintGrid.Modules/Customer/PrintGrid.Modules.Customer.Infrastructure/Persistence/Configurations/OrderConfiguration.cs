using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", "customer");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => new { o.CustomerId, o.Status });

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(o => o.PaymentTransactionId).HasMaxLength(128);

        builder.OwnsOne(o => o.TotalPrice, price =>
        {
            price.Property(p => p.Amount).HasColumnName("total_amount").HasPrecision(18, 2).IsRequired();
            price.Property(p => p.Currency).HasColumnName("total_currency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(o => o.DeliveryAddress, address =>
        {
            address.Property(a => a.Street).HasColumnName("delivery_street").HasMaxLength(200).IsRequired();
            address.Property(a => a.Ward).HasColumnName("delivery_ward").HasMaxLength(100);
            address.Property(a => a.District).HasColumnName("delivery_district").HasMaxLength(100);
            address.Property(a => a.City).HasColumnName("delivery_city").HasMaxLength(100).IsRequired();
            address.Property(a => a.PostalCode).HasColumnName("delivery_postal_code").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("delivery_country").HasMaxLength(2).IsRequired();
        });

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(o => o.DomainEvents);
    }
}
