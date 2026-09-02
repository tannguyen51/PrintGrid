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
        builder.Property(l => l.OnTimeDeliveryRate).HasPrecision(5, 4).IsRequired();
        builder.Property(l => l.FirstPassYield).HasPrecision(5, 4).IsRequired();

        builder.HasIndex(l => l.IsActive);

        builder.HasMany(l => l.Machines)
            .WithOne()
            .HasForeignKey(m => m.LabId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Machines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(l => l.DomainEvents);
    }
}
