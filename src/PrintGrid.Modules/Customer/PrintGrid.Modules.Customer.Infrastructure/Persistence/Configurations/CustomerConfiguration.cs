using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Domain.Entities.Customer>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Customer> builder)
    {
        builder.ToTable("customers", "customer");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Email).HasMaxLength(256).IsRequired();
        builder.Property(c => c.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.PhoneNumber).HasMaxLength(32);
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.Email).IsUnique();
        builder.Ignore(c => c.DomainEvents);
    }
}
