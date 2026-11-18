using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Api.Bootstrap;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;
using Testcontainers.PostgreSql;

namespace PrintGrid.IntegrationTests;

public sealed class LegacyPrinterConfigurationMigrationTests
{
    [Fact]
    public async Task Upgrade_preserves_existing_printer_configuration_and_approval_without_configuring_other_machines()
    {
        await using var postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
        await postgres.StartAsync();
        var configuration = new ConfigurationBuilder().Build();
        new ServiceCollection().AddApplicationModules(configuration);
        var options = new DbContextOptionsBuilder<PrintGridDbContext>()
            .UseNpgsql(postgres.GetConnectionString(), pg => pg.MigrationsHistoryTable("__ef_migrations_history", "shared"))
            .Options;
        await using var db = new PrintGridDbContext(options);
        await db.GetService<IMigrator>().MigrateAsync("20261111061904_AddCuraLibrarySync");

        var lab = Lab.Onboard("Legacy configuration test", "Hồ Chí Minh", 1);
        var machine = Machine.Register(lab.Id, "Legacy printer", "Legacy", PrintTechnology.Fdm,
            BuildVolume.Create(220, 220, 250), .1m, .2m, ["PLA"]);
        machine.ConfigurePrinter(new PrinterConfiguration("Legacy manufacturer", 1, 1, .4m, 1.75m,
            true, 260, 100, 6000, .08m));
        var profileId = Guid.NewGuid();
        machine.SetMatchedProfiles([profileId], true);
        machine.ReviewProfile(true, Guid.NewGuid(), null);
        var unconfigured = Machine.Register(lab.Id, "Unconfigured", "Legacy", PrintTechnology.Fdm,
            BuildVolume.Create(220, 220, 250), .1m, .2m, ["PLA"]);
        lab.AddMachine(machine);
        lab.AddMachine(unconfigured);
        db.Add(lab);
        await db.SaveChangesAsync();

        // Simulate the nullable columns created when upgrading an existing configured machine.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE scheduling.machines SET
                "PrinterConfiguration_CuraDefinitionId" = NULL,
                "PrinterConfiguration_CuraExtruderDefinitionId" = NULL,
                "PrinterConfiguration_CuraVariantId" = NULL,
                "PrinterConfiguration_CuraQualityId" = NULL
            WHERE "Id" = {machine.Id};
            """);
        db.ChangeTracker.Clear();
        var registry = new MachineProfileRegistry(db, configuration);
        (await registry.GetApprovedMachineIdsAsync(CancellationToken.None)).Should().BeEmpty();

        await db.Database.MigrateAsync();
        db.ChangeTracker.Clear();

        var restored = await db.Set<Machine>().SingleAsync(m => m.Id == machine.Id);
        restored.PrinterConfiguration.Should().NotBeNull();
        restored.PrinterConfiguration!.Manufacturer.Should().Be("Legacy manufacturer");
        restored.PrinterConfiguration.CuraDefinitionId.Should().BeEmpty();
        restored.ProfileStatus.Should().Be("Approved");
        restored.CuraProfileIds.Should().Equal(profileId);
        (await registry.GetApprovedMachineIdsAsync(CancellationToken.None)).Should().Equal(machine.Id);
        (await db.Set<Machine>().SingleAsync(m => m.Id == unconfigured.Id)).PrinterConfiguration.Should().BeNull();
    }
}
