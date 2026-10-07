using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Configurations;

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_addresses", "customer");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RecipientName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Street).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Ward).HasMaxLength(100).IsRequired();
        builder.Property(x => x.District).HasMaxLength(100).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(2).IsRequired();
        builder.HasIndex(x => new { x.CustomerId, x.IsDefault });
        builder.HasOne<global::PrintGrid.Modules.Customer.Domain.Entities.Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
