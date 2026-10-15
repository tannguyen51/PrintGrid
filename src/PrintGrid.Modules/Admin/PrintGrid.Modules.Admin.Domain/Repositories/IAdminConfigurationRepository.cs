using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.Modules.Admin.Domain.Repositories;

public interface IAdminConfigurationRepository
{
    Task<IReadOnlyList<CommercialConfigurationVersion>> GetConfigurationsAsync(CancellationToken cancellationToken = default);
    Task<CommercialConfigurationVersion?> GetConfigurationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CommercialConfigurationVersion?> GetActiveConfigurationAsync(CancellationToken cancellationToken = default);
    Task<int> GetNextConfigurationVersionAsync(CancellationToken cancellationToken = default);
    Task AddConfigurationAsync(CommercialConfigurationVersion version, CancellationToken cancellationToken = default);
    Task ActivateConfigurationAsync(CommercialConfigurationVersion version, CommercialConfigurationVersion? previousActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SharedCatalogItem>> GetCatalogAsync(string? catalogType, bool includeDeprecated, CancellationToken cancellationToken = default);
    Task<SharedCatalogItem?> GetCurrentCatalogItemAsync(string catalogType, string code, CancellationToken cancellationToken = default);
    Task AddCatalogRevisionAsync(SharedCatalogItem item, SharedCatalogItem? previous, CancellationToken cancellationToken = default);
    Task<bool> IsCatalogItemInUseAsync(string catalogType, string code, CancellationToken cancellationToken = default);
    Task DeleteCatalogItemAsync(SharedCatalogItem item, CancellationToken cancellationToken = default);
}
