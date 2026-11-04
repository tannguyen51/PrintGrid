using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Api.Bootstrap;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Admin.Application.Configuration;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Infrastructure.Slicing;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

internal static class LocalSetup
{
    public static async Task ImportRegisteredProfilesAsync(string root, CuraQuoteOptions options, Guid[] ids)
    {
        var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(root, "src/PrintGrid.Api/appsettings.json"))
            .AddJsonFile(Path.Combine(root, "src/PrintGrid.Api/appsettings.Development.json"), optional: true)
            .AddJsonFile(Path.Combine(root, "src/PrintGrid.Api/appsettings.Local.json"), optional: true).AddEnvironmentVariables().Build();
        var connection = config.GetConnectionString("DefaultConnection")!;
        var parsed = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        if (parsed.Host is not ("localhost" or "127.0.0.1" or "::1"))
            throw new InvalidOperationException("Profile import is local only.");
        new ServiceCollection().AddApplicationModules(config);
        await using var db = new PrintGridDbContext(new DbContextOptionsBuilder<PrintGridDbContext>().UseNpgsql(connection,
            pg => pg.MigrationsHistoryTable("__ef_migrations_history", "shared")).Options);
        await db.Database.MigrateAsync();
        foreach (var id in ids)
        {
            var machine = await db.Set<Machine>().SingleOrDefaultAsync(m => m.Id == id);
            var standard = id == ids[0];
            var model = machine?.Model ?? (standard ? "VIRTUAL STANDARD 220" : "VIRTUAL FAST 320");
            var width = standard ? 220m : 320m;
            if (!await db.Set<CuraProfile>().AnyAsync(p => p.Manufacturer == "PRINTGRID LOCAL TEST" && p.MachineModel == model.ToUpper() && p.WidthMm == width))
            {
                var source = options.Machines[id.ToString()];
                var profile = CuraProfileImporter.Parse(machine?.Name ?? model, "PrintGrid Local Test", model, "PLA", "1-local",
                    "5.11.0", 4, await File.ReadAllTextAsync(source.SettingsPath));
                db.Add(profile);
                if (machine is not null && machine.PrinterConfiguration is null)
                    machine.ConfigurePrinter(new("PrintGrid Local Test", 4, 4, .4m, 1.75m, true, 260, 100,
                        source.PricePerHour!.Value, source.FailureRate!.Value));
            }
        }
        await db.SaveChangesAsync();
        var matcher = new MachineProfileRegistry(db, config);
        foreach (var machine in await db.Set<Machine>().Where(m => ids.Contains(m.Id) && m.ProfileStatus != "Approved").ToListAsync())
            await matcher.MatchAsync(machine, default);
        await db.SaveChangesAsync();
        Console.WriteLine("Imported local virtual profiles into database; matching machines await Admin approval.");
    }

    public static async Task ConfigureAsync(string root, CuraQuoteOptions options)
    {
        var path = Path.Combine(root, "src/PrintGrid.Api/appsettings.Local.json");
        var config = File.Exists(path) ? JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject() : new JsonObject();
        config["QuoteSlicer"] = JsonSerializer.SerializeToNode(options);
        // Separate local database: no production or existing customer/lab data is changed.
        var connection = Environment.GetEnvironmentVariable("PRINTGRID_SLICER_LOCAL_DB")
            ?? "Host=localhost;Port=5432;Database=printgrid_slicer_local;Username=postgres;Password=postgres;Timeout=3";
        EnsureLocal(connection);
        config["ConnectionStrings"] ??= new JsonObject();
        config["ConnectionStrings"]!["DefaultConnection"] = connection;
        config["Payment"] ??= new JsonObject();
        config["Payment"]!["AutoConfirmOnPlace"] = false;
        config["Email"] ??= new JsonObject();
        config["Email"]!["Smtp"] ??= new JsonObject();
        config["Email"]!["Smtp"]!["Enabled"] = false;
        var minioKey = Environment.GetEnvironmentVariable("PRINTGRID_LOCAL_MINIO_ACCESS");
        var minioSecret = Environment.GetEnvironmentVariable("PRINTGRID_LOCAL_MINIO_SECRET");
        if (!string.IsNullOrEmpty(minioKey) && !string.IsNullOrEmpty(minioSecret))
        {
            config["MinIO"] ??= new JsonObject();
            config["MinIO"]!["AccessKey"] = minioKey;
            config["MinIO"]!["SecretKey"] = minioSecret;
        }
        await File.WriteAllTextAsync(path, config.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine("Configured: " + path);
        Console.WriteLine("Database: printgrid_slicer_local (loopback only); local settings are gitignored.");
    }

    private static void EnsureLocal(string connection)
    {
        var parsed = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        if (parsed.Host is not ("localhost" or "127.0.0.1" or "::1") || parsed.Database != "printgrid_slicer_local")
            throw new InvalidOperationException("Local seed accepts only loopback host and database printgrid_slicer_local.");
    }

    public static async Task SeedAsync(string root, Guid[] ids, string[] colors)
    {
        var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(root, "src/PrintGrid.Api/appsettings.Local.json")).Build();
        var connection = config.GetConnectionString("DefaultConnection")!;
        EnsureLocal(connection);
        // Register all module mappings exactly as the API does, without starting Hangfire/email.
        new ServiceCollection().AddApplicationModules(config);
        var dbOptions = new DbContextOptionsBuilder<PrintGridDbContext>().UseNpgsql(connection,
            pg => pg.MigrationsHistoryTable("__ef_migrations_history", "shared")).Options;
        await using var db = new PrintGridDbContext(dbOptions);
        await db.Database.MigrateAsync();
        var actor = await db.Set<Customer>().SingleOrDefaultAsync(c => c.Email == "slicer-admin@printgrid.local");
        if (actor is null)
        {
            actor = Customer.Register("slicer-admin@printgrid.local", BCrypt.Net.BCrypt.HashPassword("PrintGridLocal!2026"), "Local Slicer Admin", null);
            actor.AssignRole("Admin"); actor.VerifyEmail(); db.Add(actor);
        }
        var ops = await db.Set<Customer>().SingleOrDefaultAsync(c => c.Email == "slicer-ops@printgrid.local");
        if (ops is null)
        {
            ops = Customer.Register("slicer-ops@printgrid.local", BCrypt.Net.BCrypt.HashPassword("PrintGridLocal!2026"), "Local Slicer Ops", null);
            ops.AssignRole("OpsManager"); ops.VerifyEmail(); db.Add(ops);
        }
        var customer = await db.Set<Customer>().SingleOrDefaultAsync(c => c.Email == "slicer-customer@printgrid.local");
        if (customer is null)
        {
            customer = Customer.Register("slicer-customer@printgrid.local", BCrypt.Net.BCrypt.HashPassword("PrintGridLocal!2026"), "Local Slicer Customer", null);
            customer.VerifyEmail(); db.Add(customer);
        }
        for (var i = 0; i < 2; i++)
        {
            if (await db.Set<Machine>().AnyAsync(m => m.Id == ids[i])) continue;
            var lab = Lab.Onboard($"LOCAL TEST Lab {(i == 0 ? "Standard 220" : "Fast 320")}", "Hồ Chí Minh", 1);
            var machine = Machine.Register(lab.Id, i == 0 ? "LOCAL TEST Standard 220" : "LOCAL TEST Fast 320",
                "VIRTUAL FDM 4-tool / quotation tests only", PrintTechnology.Fdm,
                i == 0 ? BuildVolume.Create(220, 220, 250) : BuildVolume.Create(320, 320, 350), .05m, .1m, ["PLA"]);
            // Deterministic IDs bind the demo DB records to the installed profile configuration.
            typeof(Machine).GetProperty(nameof(Machine.Id))!.SetValue(machine, ids[i]);
            lab.AddMachine(machine);
            foreach (var color in colors) lab.SetMaterialStock("PLA", color, 500_000, 1000);
            db.Add(lab);
        }
        if (!await db.Set<SharedCatalogItem>().AnyAsync(c => c.CatalogType == "Material" && c.Code == "PLA"))
            db.Add(SharedCatalogItem.Create("Material", "PLA", 1, "PLA local test", null, 300m, false,
                "{\"density\":1.24}", false, actor.Id));
        foreach (var color in colors)
            if (!await db.Set<SharedCatalogItem>().AnyAsync(c => c.CatalogType == "Color" && c.Code == color))
                db.Add(SharedCatalogItem.Create("Color", color, 1, color, null, null, false, "{}", false, actor.Id));
        if (!await db.Set<CommercialConfigurationVersion>().AnyAsync(c => c.Status == "Active"))
        {
            var rates = new CommercialParametersDto(new Dictionary<string, decimal> { ["PLA"] = 300 },
                new Dictionary<string, decimal> { ["PLA"] = 1.24m }, 6000, 0, 0, 0, 100_000_000,
                48, new Dictionary<string, decimal> { ["time"] = .3m, ["load"] = .2m, ["quality"] = .2m, ["cost"] = .15m, ["logistics"] = .15m },
                100, 30, 24, new Dictionary<string, decimal> { ["PLA:BLUE"] = 300000, ["PLA:WHITE"] = 290000, ["PLA:SIGNALRED"] = 310000 }, .08m, 30000);
            var version = CommercialConfigurationVersion.Create(1, "LOCAL TEST CuraEngine PLA", JsonSerializer.Serialize(rates, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                "Virtual lab pricing for local testing only", actor.Id);
            version.Activate(); db.Add(version);
        }
        await db.SaveChangesAsync();
        Console.WriteLine("Seeded isolated local database: 2 labs/machines, colors, demo pricing and three verified test users.");
    }
}
