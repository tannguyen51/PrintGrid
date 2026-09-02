using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Analytics.Application;
using PrintGrid.SharedKernel.Modules;

namespace PrintGrid.Modules.Analytics.Infrastructure;

public class AnalyticsModule : IModule
{
    public string Name => "Analytics";

    public static Assembly ApplicationAssembly => typeof(AssemblyMarker).Assembly;

    public static Assembly InfrastructureAssembly => typeof(AnalyticsModule).Assembly;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        ModuleAssemblyRegistry.Register(InfrastructureAssembly);
        services.AddAnalyticsApplication();
        AnalyticsModuleRegistration.AddRepositories(services);
    }
}
