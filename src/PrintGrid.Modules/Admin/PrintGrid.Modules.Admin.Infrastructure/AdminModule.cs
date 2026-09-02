using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Admin.Application;
using PrintGrid.SharedKernel.Modules;

namespace PrintGrid.Modules.Admin.Infrastructure;

public class AdminModule : IModule
{
    public string Name => "Admin";

    public static Assembly ApplicationAssembly => typeof(AssemblyMarker).Assembly;

    public static Assembly InfrastructureAssembly => typeof(AdminModule).Assembly;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        ModuleAssemblyRegistry.Register(InfrastructureAssembly);
        services.AddAdminApplication();
        AdminModuleRegistration.AddRepositories(services);
    }
}
