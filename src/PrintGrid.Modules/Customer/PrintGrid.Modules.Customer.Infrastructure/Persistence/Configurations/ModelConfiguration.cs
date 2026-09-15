using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Configurations;

public class ModelConfiguration : IEntityTypeConfiguration<Model>
{
    public void Configure(EntityTypeBuilder<Model> builder)
    {
        builder.ToTable("models", "customer");
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.CustomerId, m.CreatedAt });

        builder.Property(m => m.Name).HasMaxLength(120).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.FileName).HasMaxLength(255).IsRequired();
        builder.Property(m => m.FileFormat).HasMaxLength(10).IsRequired();
        builder.Property(m => m.SizeBytes).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        // Simple postgres text[] storage for tags.
        builder.PrimitiveCollection(m => m.Tags);

        builder.Ignore(m => m.DomainEvents);
    }
}