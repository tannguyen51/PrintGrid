using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Configurations;

public sealed class DesignRequestConfiguration : IEntityTypeConfiguration<DesignRequest>
{
    public void Configure(EntityTypeBuilder<DesignRequest> builder)
    {
        builder.ToTable("design_requests", "customer");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Mirrors Machine._supportedMaterials: a private List<string> mapped as PostgreSQL text[].
        builder.Property<List<string>>("_referenceImageKeys")
            .HasColumnName("reference_image_keys")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Ignore(x => x.ReferenceImageKeys);
        builder.HasIndex(x => new { x.CustomerId, x.Status });
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });

        builder.HasOne<PrintGrid.Modules.Customer.Domain.Entities.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
