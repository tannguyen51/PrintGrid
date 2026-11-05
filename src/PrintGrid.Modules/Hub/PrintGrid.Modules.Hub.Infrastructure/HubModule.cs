using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Hub.Application;
using PrintGrid.SharedKernel.Modules;

namespace PrintGrid.Modules.Hub.Infrastructure;

public class HubModule : IModule
{
    public string Name => "Hub";

    private static Assembly InfrastructureAssembly => typeof(HubModule).Assembly;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        ModuleAssemblyRegistry.Register(InfrastructureAssembly);
        services.AddHubApplication();
        HubModuleRegistration.AddRepositories();
    }
}
