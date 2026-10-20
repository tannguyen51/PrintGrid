namespace PrintGrid.Simulator.Simulation;

/// <summary>
/// Inputs for one generated virtual network. Everything the generator needs lives here so a
/// run can be reproduced from (seed, labs, machines, orders) alone (AC TEST-003).
/// </summary>
public sealed class SimulationOptions
{
    public int Labs { get; set; } = 5;
    public int Machines { get; set; } = 15;
    public int Orders { get; set; } = 50;
    public int Seed { get; set; } = 42;

    /// <summary>UC-027 AC02 preset: 50 labs / 300 machines / 1000 orders.</summary>
    public bool Stress { get; set; }

    public static SimulationOptions StressPreset(int seed) => new()
    {
        Labs = 50,
        Machines = 300,
        Orders = 1000,
        Seed = seed,
        Stress = true
    };

    public void Validate()
    {
        if (Labs <= 0) throw new ArgumentOutOfRangeException(nameof(Labs), "--labs must be positive");
        if (Machines <= 0) throw new ArgumentOutOfRangeException(nameof(Machines), "--machines must be positive");
        if (Orders <= 0) throw new ArgumentOutOfRangeException(nameof(Orders), "--orders must be positive");
        if (Machines < Labs)
            throw new ArgumentOutOfRangeException(nameof(Machines), "--machines must be at least --labs so every lab can receive work");
    }

    public SimulationOptions Clone() => (SimulationOptions)MemberwiseClone();
}
