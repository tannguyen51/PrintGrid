using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
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
        var rates = configuration.GetSection("ShipmentBatch:ZoneRates").GetChildren()
            .ToDictionary(
                item => item.Key.ToUpperInvariant(),
                item => decimal.Parse(item.Value ?? "0", CultureInfo.InvariantCulture));
        services.AddSingleton(new Application.Services.TransportAllowancePolicy(
            rates,
            decimal.TryParse(configuration["ShipmentBatch:WeightMultipliers:UpTo5Kg"], NumberStyles.Number, CultureInfo.InvariantCulture, out var medium) ? medium : 1.6m,
            decimal.TryParse(configuration["ShipmentBatch:WeightMultipliers:Over5Kg"], NumberStyles.Number, CultureInfo.InvariantCulture, out var large) ? large : 2.4m));
        services.AddSingleton(new Application.Commands.OpsWorkflow.OpsMonitorPolicy(
            decimal.TryParse(configuration["OpsMonitor:AtRiskSlackHours"], NumberStyles.Number, CultureInfo.InvariantCulture, out var slack) ? slack : 4m));
    }
}
