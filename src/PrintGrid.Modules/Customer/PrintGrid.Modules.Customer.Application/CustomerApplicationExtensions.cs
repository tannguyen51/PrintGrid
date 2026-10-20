using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Modules.Customer.Application.Behaviors;
using PrintGrid.Modules.Customer.Application.Quotes;

namespace PrintGrid.Modules.Customer.Application;

public static class CustomerApplicationExtensions
{
    public static IServiceCollection AddCustomerApplication(this IServiceCollection services)
    {
        var assembly = typeof(AssemblyMarker).Assembly;

        services.AddMediatR(config => config.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<ICommercialPolicyProvider, DefaultCommercialPolicyProvider>();

        return services;
    }
}
