using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Modules.Scheduling.Application.Behaviors;

namespace PrintGrid.Modules.Scheduling.Application;

public static class SchedulingApplicationExtensions
{
    public static IServiceCollection AddSchedulingApplication(this IServiceCollection services)
    {
        var assembly = typeof(AssemblyMarker).Assembly;

        services.AddMediatR(config => config.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
