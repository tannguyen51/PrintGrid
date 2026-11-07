using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Customer.Application;
using PrintGrid.Modules.Customer.Infrastructure.Services;
using PrintGrid.SharedKernel.Modules;

namespace PrintGrid.Modules.Customer.Infrastructure;

public class CustomerModule : IModule
{
    public string Name => "Customer";

    private static Assembly InfrastructureAssembly => typeof(CustomerModule).Assembly;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        ModuleAssemblyRegistry.Register(InfrastructureAssembly);
        services.AddCustomerApplication();
        CustomerModuleRegistration.AddRepositories(services);
        services.AddScoped<Application.Quotes.IQuoteSlicer, Slicing.CuraQuoteSlicer>();
        services.AddScoped<IMachineFilterService, EfMachineFilterService>();
    }
}
