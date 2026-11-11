using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public sealed class CuraProfileConfiguration : IEntityTypeConfiguration<CuraProfile>
{
    public void Configure(EntityTypeBuilder<CuraProfile> builder)
    {
        builder.ToTable("cura_profiles", "scheduling");
        builder.HasKey(p => p.Id);
        foreach (var name in new[] { "Name", "Manufacturer", "MachineModel", "MaterialCode", "Version", "EngineVersion" })
            builder.Property<string>(name).HasMaxLength(150).IsRequired();
        foreach (var name in new[] { "CuraDefinitionId", "CuraExtruderDefinitionId", "CuraVariantId", "CuraQualityId" })
            builder.Property<string>(name).HasMaxLength(200).IsRequired();
        foreach (var name in new[] { "NozzleDiameterMm", "FilamentDiameterMm", "WidthMm", "DepthMm", "HeightMm", "RequiredNozzleTemperatureC", "RequiredBedTemperatureC" })
            builder.Property<decimal>(name).HasPrecision(10, 3);
        builder.Property(p => p.SettingsJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(p => new { p.Manufacturer, p.MachineModel, p.MaterialCode });
    }
}
