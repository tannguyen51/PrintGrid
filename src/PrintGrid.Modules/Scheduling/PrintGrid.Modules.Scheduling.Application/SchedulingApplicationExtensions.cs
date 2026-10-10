using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Behaviors;
using PrintGrid.Modules.Scheduling.Application.Services;

namespace PrintGrid.Modules.Scheduling.Application;

public static class SchedulingApplicationExtensions
{
    public static IServiceCollection AddSchedulingApplication(this IServiceCollection services)
    {
        var assembly = typeof(AssemblyMarker).Assembly;

        services.AddMediatR(config => config.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // The assign engine is the one seam behind manual assignment, event-driven
        // rescheduling and urgent reprints (FR-SCHED-006/007, FR-HUB-003).
        services.AddScoped<IAssignmentEngine, AssignmentEngine>();
        services.AddScoped<IPlacementShortfallService, PlacementShortfallService>();

        return services;
    }
}
