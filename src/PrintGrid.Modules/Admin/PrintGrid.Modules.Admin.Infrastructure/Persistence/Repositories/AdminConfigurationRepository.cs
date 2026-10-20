using System.Data;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;

namespace PrintGrid.Modules.Admin.Infrastructure.Persistence.Repositories;

public sealed class AdminConfigurationRepository(PrintGridDbContext context) : IAdminConfigurationRepository
{
    public async Task<IReadOnlyList<CommercialConfigurationVersion>> GetConfigurationsAsync(CancellationToken cancellationToken = default) =>
        await context.Set<CommercialConfigurationVersion>().AsNoTracking()
            .OrderByDescending(x => x.VersionNumber).ToListAsync(cancellationToken);

    public Task<CommercialConfigurationVersion?> GetConfigurationAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<CommercialConfigurationVersion>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<CommercialConfigurationVersion?> GetActiveConfigurationAsync(CancellationToken cancellationToken = default) =>
        context.Set<CommercialConfigurationVersion>().FirstOrDefaultAsync(x => x.Status == ConfigurationStatuses.Active, cancellationToken);

    public async Task<int> GetNextConfigurationVersionAsync(CancellationToken cancellationToken = default) =>
        (await context.Set<CommercialConfigurationVersion>().MaxAsync(x => (int?)x.VersionNumber, cancellationToken) ?? 0) + 1;

    public async Task AddConfigurationAsync(CommercialConfigurationVersion version, CancellationToken cancellationToken = default)
    {
        await context.Set<CommercialConfigurationVersion>().AddAsync(version, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateConfigurationAsync(CommercialConfigurationVersion version, CommercialConfigurationVersion? previousActive, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (previousActive is not null)
        {
            previousActive.Archive();
            await context.SaveChangesAsync(cancellationToken);
        }
        version.Activate();
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SharedCatalogItem>> GetCatalogAsync(string? catalogType, bool includeDeprecated, CancellationToken cancellationToken = default)
    {
        var query = context.Set<SharedCatalogItem>().AsNoTracking().Where(x => x.IsCurrent);
        if (!string.IsNullOrWhiteSpace(catalogType)) query = query.Where(x => x.CatalogType == catalogType);
        if (!includeDeprecated) query = query.Where(x => !x.IsDeprecated);
        return await query.OrderBy(x => x.CatalogType).ThenBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<SharedCatalogItem?> GetCurrentCatalogItemAsync(string catalogType, string code, CancellationToken cancellationToken = default) =>
        context.Set<SharedCatalogItem>().FirstOrDefaultAsync(
            x => x.CatalogType == catalogType && x.Code == code && x.IsCurrent, cancellationToken);

    public async Task AddCatalogRevisionAsync(SharedCatalogItem item, SharedCatalogItem? previous, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (previous is not null)
        {
            previous.Supersede();
            await context.SaveChangesAsync(cancellationToken);
        }
        await context.Set<SharedCatalogItem>().AddAsync(item, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> IsCatalogItemInUseAsync(string catalogType, string code, CancellationToken cancellationToken = default)
    {
        var sql = catalogType switch
        {
            SharedCatalogTypes.Material => "SELECT EXISTS (SELECT 1 FROM customer.quote_items WHERE material_code = @code UNION ALL SELECT 1 FROM customer.order_items WHERE material_code = @code UNION ALL SELECT 1 FROM scheduling.jobs WHERE material_code = @code)",
            SharedCatalogTypes.Color => "SELECT EXISTS (SELECT 1 FROM customer.quote_items WHERE color_code = @code UNION ALL SELECT 1 FROM customer.order_items WHERE color_code = @code UNION ALL SELECT 1 FROM scheduling.jobs WHERE color_code = @code)",
            SharedCatalogTypes.Technology => "SELECT EXISTS (SELECT 1 FROM scheduling.jobs WHERE technology = @code)",
            _ => null
        };
        if (sql is null) return false;

        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "code";
            parameter.Value = code;
            command.Parameters.Add(parameter);
            return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    public async Task DeleteCatalogItemAsync(SharedCatalogItem item, CancellationToken cancellationToken = default)
    {
        context.Set<SharedCatalogItem>().Remove(item);
        await context.SaveChangesAsync(cancellationToken);
    }
}
