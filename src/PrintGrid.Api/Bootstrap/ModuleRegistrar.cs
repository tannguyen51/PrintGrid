using PrintGrid.Modules.Admin.Infrastructure;
using PrintGrid.Modules.Analytics.Infrastructure;
using PrintGrid.Modules.Customer.Infrastructure;
using PrintGrid.Modules.Hub.Infrastructure;
using PrintGrid.Modules.Lab.Infrastructure;
using PrintGrid.Modules.Scheduling.Infrastructure;
using PrintGrid.SharedKernel.Modules;

namespace PrintGrid.Api.Bootstrap;

public static class ModuleRegistrar
{
    private static readonly IModule[] Modules =
    {
        new CustomerModule(),
        new SchedulingModule(),
        new LabModule(),
        new HubModule(),
        new AnalyticsModule(),
        new AdminModule()
    };

    public static IServiceCollection AddApplicationModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        foreach (var module in Modules)
        {
            module.RegisterServices(services, configuration);
        }

        return services;
    }
}
