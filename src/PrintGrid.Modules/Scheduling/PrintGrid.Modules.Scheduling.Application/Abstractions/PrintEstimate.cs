namespace PrintGrid.Modules.Scheduling.Application.Abstractions;

/// <summary>
/// Slicing-derived production estimate for a configuration (FR-SCHED-002).
/// Minutes is the base estimate; the machine's SpeedFactor is applied by the scheduler.
/// </summary>
public record PrintEstimate(int PrintMinutes, decimal MaterialGrams);