using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;
using PrintGrid.Modules.Scheduling.Infrastructure.Scheduling;

namespace PrintGrid.Modules.Scheduling.Infrastructure;

internal static class SchedulingModuleRegistration
{
    internal static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<ILabRepository, LabRepository>();
        services.AddScoped<IMachineTimelineService, MachineTimelineService>();

        services.AddSingleton(ScoringWeights.Default);
        services.AddSingleton<CapabilityFilter>();
        services.AddSingleton<AssignmentScorer>();
    }
}
