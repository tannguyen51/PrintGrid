using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Admin.Domain.Repositories;
using PrintGrid.Modules.Admin.Infrastructure.Persistence.Repositories;
using PrintGrid.Modules.Customer.Application.Quotes;

namespace PrintGrid.Modules.Admin.Infrastructure;

internal static class AdminModuleRegistration
{
    internal static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAdminConfigurationRepository, AdminConfigurationRepository>();
        services.AddScoped<IQcKnowledgeRepository, QcKnowledgeRepository>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<ICommercialPolicyProvider, CommercialPolicyProvider>();
    }
}
