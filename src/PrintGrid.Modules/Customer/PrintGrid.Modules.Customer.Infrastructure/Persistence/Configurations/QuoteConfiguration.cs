using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Configurations;

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("quotes", "customer");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(q => q.FailureReason).HasMaxLength(500);
        builder.HasIndex(q => new { q.CustomerId, q.Status });
        builder.HasIndex(q => q.ExpiresAt);

        builder.OwnsOne(q => q.TotalPrice, price =>
        {
            price.Property(p => p.Amount).HasColumnName("total_amount").HasPrecision(18, 2).IsRequired();
            price.Property(p => p.Currency).HasColumnName("total_currency").HasMaxLength(3).IsRequired();
        });

        builder.HasMany(q => q.Items)
            .WithOne()
            .HasForeignKey(i => i.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(q => q.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(q => q.DomainEvents);
    }
}
