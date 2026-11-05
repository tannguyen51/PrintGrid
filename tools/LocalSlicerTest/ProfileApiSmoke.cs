using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Api.Bootstrap;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Entities;

internal static class ProfileApiSmoke
{
    public static async Task RunAsync(string root)
    {
        var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(root, "src/PrintGrid.Api/appsettings.json"))
            .AddJsonFile(Path.Combine(root, "src/PrintGrid.Api/appsettings.Development.json"), optional: true)
            .AddJsonFile(Path.Combine(root, "src/PrintGrid.Api/appsettings.Local.json"), optional: true).Build();
        var connection = config.GetConnectionString("DefaultConnection")!;
        var parsed = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        if (parsed.Host is not ("localhost" or "127.0.0.1" or "::1") || parsed.Database != "printgrid_slicer_local")
            throw new InvalidOperationException("API smoke is limited to the isolated printgrid_slicer_local database.");
        new ServiceCollection().AddApplicationModules(config);
        await using var db = new PrintGridDbContext(new DbContextOptionsBuilder<PrintGridDbContext>().UseNpgsql(connection,
            pg => pg.MigrationsHistoryTable("__ef_migrations_history", "shared")).Options);
        var password = Guid.NewGuid().ToString("N") + "aA!1";
        var actor = Customer.Register($"profile-smoke-{Guid.NewGuid():N}@printgrid.local", BCrypt.Net.BCrypt.HashPassword(password), "Temporary profile smoke", null);
        actor.AssignRole("Admin"); actor.VerifyEmail(); db.Add(actor); await db.SaveChangesAsync();
        var lab = Lab.Onboard("TEMP registry API smoke", "Hồ Chí Minh", 1);
        lab.ClaimOwnership(actor.Id); db.Add(lab); await db.SaveChangesAsync();
        try
        {
            var start = new ProcessStartInfo("pwsh") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-File");
            start.ArgumentList.Add(Path.Combine(root, "tools/local-slicer/Test-MachineRegistration.ps1"));
            start.Environment["PRINTGRID_PROFILE_TEST_EMAIL"] = actor.Email;
            start.Environment["PRINTGRID_PROFILE_TEST_PASSWORD"] = password;
            start.Environment["PRINTGRID_PROFILE_TEST_LAB"] = lab.Id.ToString();
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Console.WriteLine(await output); Console.WriteLine(await error);
            if (process.ExitCode != 0) throw new InvalidOperationException("Registry API smoke failed.");
        }
        finally
        {
            await db.Set<CuraProfile>().Where(p => p.Name == "TEMP registry API smoke profile").ExecuteDeleteAsync();
            db.Remove(lab);
            db.Remove(actor); await db.SaveChangesAsync();
            Console.WriteLine("Removed temporary API smoke-test user.");
        }
    }
}
