using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Lab.Application;
using PrintGrid.SharedKernel.Modules;

namespace PrintGrid.Modules.Lab.Infrastructure;

public class LabModule : IModule
{
    public string Name => "Lab";

    private static Assembly InfrastructureAssembly => typeof(LabModule).Assembly;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        ModuleAssemblyRegistry.Register(InfrastructureAssembly);
        services.AddLabApplication();
        LabModuleRegistration.AddRepositories();
    }
}
