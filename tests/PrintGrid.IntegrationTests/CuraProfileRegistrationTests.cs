using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PrintGrid.Api.Bootstrap;
using PrintGrid.Api.Controllers;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;
using PrintGrid.SharedKernel.Interfaces;
using Testcontainers.PostgreSql;

namespace PrintGrid.IntegrationTests;

public class CuraProfileRegistrationTests
{
    private const string Settings = """
        {"global":{"machine_extruder_count":1,"machine_width":220,"machine_depth":220,"machine_height":250,
        "machine_shape":"rectangular","machine_heated_bed":true,"machine_center_is_zero":false,
        "machine_max_feedrate_x":150,"machine_max_feedrate_y":150,"machine_acceleration":500,"speed_print":50,
        "layer_height":0.2,"machine_nozzle_size":0.4,"material_diameter":1.75},
        "extruder.0":{"machine_nozzle_size":0.4,"material_diameter":1.75,"speed_print":50,"speed_wall":25,
        "speed_travel":100,"layer_height":0.2,"material_print_temperature":205,"material_print_temperature_layer_0":210,
        "material_bed_temperature":60,"material_bed_temperature_layer_0":60}}
        """;

    [Theory]
    [InlineData(false, "Dịch vụ báo giá tự động chưa sẵn sàng")]
    [InlineData(true, "Chưa có máy in được quản trị viên duyệt")]
    public async Task Unavailable_quotation_explains_the_missing_prerequisite(bool hasSlicer, string expectedMessage)
    {
        var customerId = Guid.NewGuid();
        var model = PrintGrid.Modules.Customer.Domain.Entities.Model.Create(customerId, "Part", null,
            "part.stl", "STL", 1000, null, "printgrid-models/part.stl", "hash");
        model.ApplyGeometry(20, 20, 20, 8, 60, 10, true, true, true);
        model.Approve(null, DateTime.UtcNow);
        var models = Substitute.For<IModelRepository>();
        models.GetByIdForCustomerAsync(customerId, model.Id, Arg.Any<CancellationToken>()).Returns(model);
        var quotes = Substitute.For<IQuoteRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var slicer = Substitute.For<IQuoteSlicer>();
        slicer.GetConfiguredMachineIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>());
        var handler = new CreateQuoteCommandHandler(models, quotes, unitOfWork,
            Substitute.For<IDateTimeProvider>(), Substitute.For<IProductionCapacityProbe>(),
            Substitute.For<IMachineFilterService>(), quoteSlicer: hasSlicer ? slicer : null);

        var result = await handler.Handle(new CreateQuoteCommand(customerId, model.Id, "PLA", "BLACK",
            .2m, 20, 1, .2m, SizeCode: "ORIGINAL"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        result.Error.Message.Should().Contain(expectedMessage);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Profile_import_enables_machine_review_and_persists_activation_in_postgres()
    {
        await using var postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
        await postgres.StartAsync();
        var configuration = new ConfigurationBuilder().Build();
        new ServiceCollection().AddApplicationModules(configuration);
        var options = new DbContextOptionsBuilder<PrintGridDbContext>()
            .UseNpgsql(postgres.GetConnectionString(), pg => pg.MigrationsHistoryTable("__ef_migrations_history", "shared"))
            .Options;
        await using var db = new PrintGridDbContext(options);
        await db.Database.MigrateAsync();

        var lab = Lab.Onboard("Profile registration test", "Hồ Chí Minh", 1);
        var machine = Machine.Register(lab.Id, "Printer 1", "TEST-220", PrintTechnology.Fdm,
            BuildVolume.Create(220, 220, 250), .1m, .2m, ["PLA"]);
        machine.ConfigurePrinter(new PrinterConfiguration("Test Lab", 1, 1, .4m, 1.75m, true, 260, 100, 6000, .08m));
        lab.AddMachine(machine);
        db.Add(lab);
        await db.SaveChangesAsync();

        var registry = new MachineProfileRegistry(db, configuration);
        var controller = new CuraProfilesController(db, registry, configuration, Substitute.For<IAuditLogWriter>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                    ], "TestAdmin"))
                }
            }
        };

        var imported = await controller.Import(new ImportCuraProfileRequest("Standard PLA", "Test Lab", "TEST-220",
            "PLA", "1", "5.11.0", 1, Settings), CancellationToken.None);
        imported.Should().BeOfType<OkObjectResult>();
        var profile = await db.Set<CuraProfile>().SingleAsync();
        (await db.Database.SqlQueryRaw<bool>("SELECT \"IsActive\" AS \"Value\" FROM scheduling.cura_profiles").SingleAsync())
            .Should().BeTrue();
        machine.ProfileStatus.Should().Be("PendingReview");
        machine.CuraProfileIds.Should().Equal(profile.Id);
        (await registry.GetApprovedMachineIdsAsync(CancellationToken.None)).Should().BeEmpty();

        var reviewed = await controller.Review(machine.Id, new ReviewMachineProfileRequest(true, null), CancellationToken.None);
        reviewed.Should().BeOfType<OkObjectResult>();
        (await registry.GetApprovedMachineIdsAsync(CancellationToken.None)).Should().Contain(machine.Id);
        var resolved = await registry.ResolveAsync(machine.Id, "PLA", CancellationToken.None);
        resolved.Should().NotBeNull();
        JsonNode.DeepEquals(JsonNode.Parse(resolved!.SettingsJson), JsonNode.Parse(profile.SettingsJson)).Should().BeTrue();

        // Persisted inactive profiles must disappear from the library and cannot be used for slicing.
        db.Entry(profile).Property("IsActive").CurrentValue = false;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var list = (OkObjectResult)await controller.List(CancellationToken.None);
        JsonSerializer.SerializeToElement(list.Value).GetArrayLength().Should().Be(0);
        (await registry.ResolveAsync(machine.Id, "PLA", CancellationToken.None)).Should().BeNull();
        var reloadedMachine = await db.Set<Machine>().SingleAsync(m => m.Id == machine.Id);
        await registry.MatchAsync(reloadedMachine, CancellationToken.None);
        reloadedMachine.ProfileStatus.Should().Be("MissingProfile");
        reloadedMachine.CuraProfileIds.Should().BeEmpty();
    }
}
