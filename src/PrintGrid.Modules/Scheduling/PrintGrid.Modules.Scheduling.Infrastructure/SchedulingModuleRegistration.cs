using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;
using PrintGrid.Modules.Scheduling.Infrastructure.Scheduling;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Scheduling.Infrastructure;

internal static class SchedulingModuleRegistration
{
    internal static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<ICalibrationSampleRepository, CalibrationSampleRepository>();
        services.AddScoped<IShipmentBatchRepository, ShipmentBatchRepository>();
        services.AddScoped<ILabRepository, LabRepository>();
        services.AddScoped<IDateChangeRequestRepository, DateChangeRequestRepository>();
        services.AddScoped<IMachineTimelineService, MachineTimelineService>();
        services.AddScoped<IProductionCapacityProbe, ProductionCapacityProbe>();
        services.AddScoped<IAssignmentDecisionRepository, AssignmentDecisionRepository>();
        services.AddScoped<IOpsEscalationRepository, OpsEscalationRepository>();
        services.AddScoped<ScoringWeightConfigurationRepository>();
        services.AddScoped<IScoringWeightConfigurationRepository>(sp => sp.GetRequiredService<ScoringWeightConfigurationRepository>());
        services.AddScoped<IScoringConfigurationProvider>(sp => sp.GetRequiredService<ScoringWeightConfigurationRepository>());

        services.AddSingleton<ISlicingService, PrintSlicingService>();

        // The scoring weights are versioned ("kỳ cấu hình", FR-SCHED-009 / BR-CONFIG-003); the
        // active set is frozen into every decision log entry so past scores stay reproducible.
        services.AddSingleton<CapabilityFilter>();
    }
}
