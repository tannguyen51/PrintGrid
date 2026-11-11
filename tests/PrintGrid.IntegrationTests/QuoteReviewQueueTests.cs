using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using PrintGrid.Api.Bootstrap;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Infrastructure.Shared.Persistence.Migrations;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;
using PrintGrid.SharedKernel.ValueObjects;
using Testcontainers.PostgreSql;

namespace PrintGrid.IntegrationTests;

public class QuoteReviewQueueTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Production_upgrade_restores_ops_queue_and_approval(int manualPatch)
    {
        await using var postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
        await postgres.StartAsync();
        new ServiceCollection().AddApplicationModules(new ConfigurationBuilder().Build());
        var options = new DbContextOptionsBuilder<PrintGridDbContext>()
            .UseNpgsql(postgres.GetConnectionString(), pg => pg.MigrationsHistoryTable("__ef_migrations_history", "shared"))
            .Options;
        await using var db = new PrintGridDbContext(options);
        await db.GetService<IMigrator>().MigrateAsync("20261104152844_AddMachineCuraProfileRegistry");

        // Existing drafts must survive the upgrade with their original amounts/items.
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(48));
        var item = quote.AddItem(Guid.NewGuid(), 2,
            PrintConfiguration.Create("PLA", "BLACK", .2m, 20, .2m),
            Money.Of(250_000m), 60, 20m, 125_000m, 125_000m, 10, 10, 10);
        quote.MarkDraft(new DateOnly(2026, 11, 21), "test-1");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO customer.quotes ("Id", "CustomerId", "Status", "CreatedAt", "PromisedDeliveryDate",
                "PricingVersion", "EngineTotalAmount", "EnginePromisedDeliveryDate", "AutoApproved", total_amount, total_currency)
            VALUES ({quote.Id}, {quote.CustomerId}, 'Draft', {quote.CreatedAt}, {quote.PromisedDeliveryDate},
                {quote.PricingVersion}, {quote.TotalPrice.Amount}, {quote.PromisedDeliveryDate}, FALSE, {quote.TotalPrice.Amount}, 'VND');
            INSERT INTO customer.quote_items ("Id", "QuoteId", "ModelId", "Quantity", "EstimatedPrintMinutes",
                "EstimatedMaterialGrams", "MaterialCostAmount", "MachineTimeCostAmount", unit_amount, unit_currency,
                material_code, color_code, layer_height_mm, infill_percent, tolerance_mm)
            VALUES ({item.Id}, {quote.Id}, {item.ModelId}, 2, 60, 20, 125000, 125000, 250000, 'VND', 'PLA', 'BLACK', 0.2, 20, 0.2);
            """);

        if (manualPatch > 0)
        {
            // Reproduce environments where the undiscovered migration was run by hand.
            var commands = db.GetService<IMigrationsSqlGenerator>()
                .Generate(new AddCustomerModelSelection().UpOperations);
            foreach (var command in commands)
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
            if (manualPatch == 2)
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE customer.quote_items RENAME COLUMN profile_version TO \"ProfileVersion\"");
            if (manualPatch == 2)
                await db.Database.ExecuteSqlRawAsync("UPDATE customer.quote_items SET \"ProfileVersion\" = 'legacy-1'");
            else
                await db.Database.ExecuteSqlRawAsync("UPDATE customer.quote_items SET profile_version = 'legacy-1'");
        }

        var readBrokenQueue = () => new QuoteRepository(db).GetDraftsAsync();
        var failure = await readBrokenQueue.Should().ThrowAsync<PostgresException>();
        failure.Which.SqlState.Should().Be(PostgresErrorCodes.UndefinedColumn);

        var migrationStart = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        migrationStart.ArgumentList.Add(typeof(Program).Assembly.Location);
        migrationStart.ArgumentList.Add("--migrate");
        migrationStart.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        migrationStart.Environment["ConnectionStrings__DefaultConnection"] = postgres.GetConnectionString();
        using var migration = Process.Start(migrationStart)!;
        var output = migration.StandardOutput.ReadToEndAsync();
        var error = migration.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try { await migration.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { migration.Kill(entireProcessTree: true); throw; }
        migration.ExitCode.Should().Be(0, "the pre-deploy command must exit successfully: {0} {1}", await output, await error);
        // The repair also tolerates columns/tables already present on an installation.
        var repairCommands = db.GetService<IMigrationsSqlGenerator>()
            .Generate(new RepairQuoteReviewSchema().UpOperations);
        foreach (var command in repairCommands)
            await db.Database.ExecuteSqlRawAsync(command.CommandText);

        var upgraded = await new QuoteRepository(db).GetByIdAsync(quote.Id);
        upgraded!.TotalPrice.Amount.Should().Be(500_000);
        upgraded.Items.Single().ProfileVersion.Should().Be(manualPatch > 0 ? "legacy-1" : null);
        upgraded.Items.Single().FreezePinchData(Guid.NewGuid(), "verified-1", 1.1m, 450_000, new DateOnly(2026, 11, 20));
        upgraded.SetPinnedModelContext(1.1m, Guid.NewGuid(), 450_000, new DateOnly(2026, 11, 20));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var saved = await new QuoteRepository(db).GetByIdAsync(quote.Id);
        saved!.Items.Single().ProfileVersion.Should().Be("verified-1");

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:DefaultConnection", postgres.GetConnectionString());
            builder.UseSetting("HttpsRedirection:Enabled", "false");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = postgres.GetConnectionString(),
                ["HttpsRedirection:Enabled"] = "false"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.AddAuthentication(auth =>
                {
                    auth.DefaultAuthenticateScheme = "QueueTest";
                    auth.DefaultChallengeScheme = "QueueTest";
                    auth.DefaultForbidScheme = "QueueTest";
                }).AddScheme<AuthenticationSchemeOptions, QueueTestAuthHandler>("QueueTest", _ => { });
            });
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "OpsManager");
        var response = await client.GetAsync("/api/v1/quote-reviews");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var queue = await response.Content.ReadFromJsonAsync<List<QuoteDto>>();
        queue.Should().ContainSingle().Which.Id.Should().Be(quote.Id);
        queue![0].Items.Should().HaveCount(1);

        var approval = await client.PostAsJsonAsync($"/api/v1/quote-reviews/{quote.Id}/approve", new
        {
            totalPrice = quote.TotalPrice.Amount,
            promisedDeliveryDate = quote.PromisedDeliveryDate
        });
        approval.StatusCode.Should().Be(HttpStatusCode.OK, await approval.Content.ReadAsStringAsync());
        (await client.GetFromJsonAsync<List<QuoteDto>>("/api/v1/quote-reviews")).Should().BeEmpty();

        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Customer");
        (await client.GetAsync("/api/v1/quote-reviews")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed class QueueTestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
                new Claim(ClaimTypes.Role, Request.Headers["X-Test-Role"].ToString())
            ], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
