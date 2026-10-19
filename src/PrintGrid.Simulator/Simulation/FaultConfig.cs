namespace PrintGrid.Simulator.Simulation;

/// <summary>
/// Four independently toggleable fault types (RUN-06). A rate of zero disables that fault;
/// each type draws from its own seeded stream so switching one on does not shift another.
/// </summary>
public sealed record FaultConfig(
    bool Enabled,
    double DeclineRate,
    double PrintFailureRate,
    double BreakdownRate,
    double MeanDowntimeMinutes,
    double InspectionFailRate)
{
    public static FaultConfig Default { get; } = new(
        Enabled: true,
        DeclineRate: 0.05,
        PrintFailureRate: 0.04,
        BreakdownRate: 0.02,
        MeanDowntimeMinutes: 120,
        InspectionFailRate: 0.04);

    public static FaultConfig None { get; } = new(false, 0, 0, 0, 0, 0);

    public bool DeclineEnabled => Enabled && DeclineRate > 0;
    public bool PrintFailureEnabled => Enabled && PrintFailureRate > 0;
    public bool BreakdownEnabled => Enabled && BreakdownRate > 0;
    public bool InspectionFailEnabled => Enabled && InspectionFailRate > 0;

    public FaultConfig With(
        double? declineRate = null,
        double? printFailureRate = null,
        double? breakdownRate = null,
        double? meanDowntimeMinutes = null,
        double? inspectionFailRate = null)
    {
        return this with
        {
            Enabled = true,
            DeclineRate = declineRate ?? DeclineRate,
            PrintFailureRate = printFailureRate ?? PrintFailureRate,
            BreakdownRate = breakdownRate ?? BreakdownRate,
            MeanDowntimeMinutes = meanDowntimeMinutes ?? MeanDowntimeMinutes,
            InspectionFailRate = inspectionFailRate ?? InspectionFailRate
        };
    }

    public FaultConfig DisableAll() => this with { Enabled = false };

    public void Validate()
    {
        ValidateRate(DeclineRate, nameof(DeclineRate));
        ValidateRate(PrintFailureRate, nameof(PrintFailureRate));
        ValidateRate(BreakdownRate, nameof(BreakdownRate));
        ValidateRate(InspectionFailRate, nameof(InspectionFailRate));
        if (MeanDowntimeMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(MeanDowntimeMinutes), "Mean downtime cannot be negative");
    }

    private static void ValidateRate(double rate, string name)
    {
        if (rate is < 0 or > 1)
            throw new ArgumentOutOfRangeException(name, "A fault rate must stay between 0 and 1");
    }

    public override string ToString() =>
        Enabled
            ? $"decline={DeclineRate:P0} print={PrintFailureRate:P0} breakdown={BreakdownRate:P0} downtimeMean={MeanDowntimeMinutes:0}m inspection={InspectionFailRate:P0}"
            : "off";
}
