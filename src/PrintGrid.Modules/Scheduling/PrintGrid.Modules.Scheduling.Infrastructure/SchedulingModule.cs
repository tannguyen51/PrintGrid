using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application;
using PrintGrid.SharedKernel.Modules;

namespace PrintGrid.Modules.Scheduling.Infrastructure;

public class SchedulingModule : IModule
{
    public string Name => "Scheduling";

    public static Assembly ApplicationAssembly => typeof(AssemblyMarker).Assembly;

    public static Assembly InfrastructureAssembly => typeof(SchedulingModule).Assembly;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        ModuleAssemblyRegistry.Register(InfrastructureAssembly);
        services.AddSchedulingApplication();
        SchedulingModuleRegistration.AddRepositories(services);
    }
}
