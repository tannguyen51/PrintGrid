using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public class MaterialReservationConfiguration : IEntityTypeConfiguration<MaterialReservation>
{
    public void Configure(EntityTypeBuilder<MaterialReservation> builder)
    {
        builder.ToTable("material_reservations", "scheduling");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReservedGrams).HasPrecision(12, 2).IsRequired();
        builder.HasIndex(x => x.JobId).IsUnique();
    }
}
