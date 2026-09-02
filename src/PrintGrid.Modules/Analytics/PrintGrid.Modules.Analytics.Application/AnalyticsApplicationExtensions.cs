using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Modules.Analytics.Application.Behaviors;

namespace PrintGrid.Modules.Analytics.Application;

public static class AnalyticsApplicationExtensions
{
    public static IServiceCollection AddAnalyticsApplication(this IServiceCollection services)
    {
        var assembly = typeof(AssemblyMarker).Assembly;

        services.AddMediatR(config => config.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
