using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Modules.Lab.Application.Behaviors;

namespace PrintGrid.Modules.Lab.Application;

public static class LabApplicationExtensions
{
    public static IServiceCollection AddLabApplication(this IServiceCollection services)
    {
        var assembly = typeof(AssemblyMarker).Assembly;

        services.AddMediatR(config => config.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
