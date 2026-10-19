using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Simulator.Simulation;

/// <summary>The seed-stable description of one generated order, used for output and hashing.</summary>
public sealed record OrderProfile(
    string Technology,
    string Material,
    string Color,
    decimal WidthMm,
    decimal DepthMm,
    decimal HeightMm,
    decimal LayerHeightMm,
    decimal ToleranceMm,
    decimal Grams,
    int EstimatedMinutes,
    int DueLeadDays,
    string Priority);

public sealed class SimOrder
{
    public required int Index { get; init; }
    public required Job Job { get; init; }
    public required DateTime ArrivalUtc { get; init; }
    public required OrderProfile Profile { get; init; }
}

/// <summary>The virtual network plus the orders generated on it.</summary>
public sealed class GeneratedNetwork
{
    public required int Seed { get; init; }
    public required DateTime EpochUtc { get; init; }
    public required IReadOnlyList<Lab> Labs { get; init; }
    public required IReadOnlyList<Machine> Machines { get; init; }
    public required IReadOnlyList<SimOrder> Orders { get; init; }
    public required string OrderListHash { get; init; }
}
