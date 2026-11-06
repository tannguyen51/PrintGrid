using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PrintGrid.Infrastructure.Shared.Persistence;

namespace PrintGrid.Api.Bootstrap;

/// <summary>
/// EF tooling needs module mappings, not the running API, Hangfire or Redis.
/// In particular, generating the image's migration script must work without a database.
/// </summary>
public sealed class PrintGridDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PrintGridDbContext>
{
    public PrintGridDbContext CreateDbContext(string[] args)
    {
        var sourceDirectory = Path.Combine(Directory.GetCurrentDirectory(), "src", "PrintGrid.Api");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = Directory.Exists(sourceDirectory) ? sourceDirectory : Directory.GetCurrentDirectory()
        });

        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true)
                .AddEnvironmentVariables();
        }

        builder.Services.AddApplicationModules(builder.Configuration);

        var options = new DbContextOptionsBuilder<PrintGridDbContext>()
            .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "shared"))
            .Options;
        return new PrintGridDbContext(options);
    }
}
