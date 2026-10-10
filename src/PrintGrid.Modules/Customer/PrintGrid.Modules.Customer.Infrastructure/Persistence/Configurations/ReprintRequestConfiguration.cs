using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Configurations;

public class ReprintRequestConfiguration : IEntityTypeConfiguration<ReprintRequest>
{
    public void Configure(EntityTypeBuilder<ReprintRequest> builder)
    {
        builder.ToTable("reprint_requests", "customer");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Photos).HasColumnType("text[]").IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResolutionNote).HasMaxLength(2000);
        builder.HasIndex(x => new { x.OrderId, x.CreatedAt });
        builder.HasIndex(x => new { x.CustomerId, x.Status });
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PrintGrid.Modules.Customer.Domain.Entities.Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
