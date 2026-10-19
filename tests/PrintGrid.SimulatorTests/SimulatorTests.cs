using PrintGrid.Simulator.Simulation;

namespace PrintGrid.SimulatorTests;

public sealed class SimulatorTests
{
    private static SimulationOptions SmallOptions(int seed = 42) => new()
    {
        Labs = 3,
        Machines = 6,
        Orders = 20,
        Seed = seed
    };

    [Fact]
    public void Generate_SameSeed_ProducesIdenticalOrdersAndHash()
    {
        var options = SmallOptions();

        var first = NetworkGenerator.Generate(options);
        var second = NetworkGenerator.Generate(options);

        first.OrderListHash.Should().Be(second.OrderListHash);
        ReportWriter.RenderGenerate(first, options)
            .Should().Be(ReportWriter.RenderGenerate(second, options));
    }

    [Fact]
    public void Generate_DifferentSeed_ChangesTheOrderList()
    {
        var first = NetworkGenerator.Generate(SmallOptions(seed: 42));
        var second = NetworkGenerator.Generate(SmallOptions(seed: 43));

        first.OrderListHash.Should().NotBe(second.OrderListHash);
    }

    [Fact]
    public void FaultToggles_AreIndependent()
    {
        var options = new SimulationOptions { Labs = 2, Machines = 8, Orders = 40, Seed = 11 };

        var declineOnly = RunWith(FaultConfig.None.With(declineRate: 0.5));
        declineOnly.Declines.Should().BeGreaterThan(0);
        declineOnly.PrintFailures.Should().Be(0);
        declineOnly.Breakdowns.Should().Be(0);
        declineOnly.InspectionFailures.Should().Be(0);

        var printOnly = RunWith(FaultConfig.None.With(printFailureRate: 1.0));
        printOnly.PrintFailures.Should().BeGreaterThan(0);
        printOnly.Declines.Should().Be(0);
        printOnly.Breakdowns.Should().Be(0);
        printOnly.InspectionFailures.Should().Be(0);

        var breakdownOnly = RunWith(FaultConfig.None.With(breakdownRate: 1.0, meanDowntimeMinutes: 60));
        breakdownOnly.Breakdowns.Should().BeGreaterThan(0);
        breakdownOnly.Declines.Should().Be(0);
        breakdownOnly.PrintFailures.Should().Be(0);
        breakdownOnly.InspectionFailures.Should().Be(0);

        var inspectionOnly = RunWith(FaultConfig.None.With(inspectionFailRate: 1.0));
        inspectionOnly.InspectionFailures.Should().BeGreaterThan(0);
        inspectionOnly.Declines.Should().Be(0);
        inspectionOnly.PrintFailures.Should().Be(0);
        inspectionOnly.Breakdowns.Should().Be(0);

        SimulationMetrics RunWith(FaultConfig faults)
        {
            var report = SimulationRunner.Run(options, [SchedulingStrategy.Capacity], faults);
            return report.Strategies.Single().WithFaults;
        }
    }

    [Fact]
    public void StrategyComparison_ProducesFourRows()
    {
        var options = SmallOptions(seed: 7);

        var report = SimulationRunner.Run(
            options,
            [SchedulingStrategy.Capacity, SchedulingStrategy.Edd, SchedulingStrategy.Nearest, SchedulingStrategy.Random],
            FaultConfig.Default);

        report.Strategies.Should().HaveCount(4);
        report.Strategies.Select(s => s.WithFaults.Strategy)
            .Should().BeEquivalentTo(["capacity", "edd", "nearest", "random"]);
        report.Strategies.Should().OnlyContain(s => s.WithFaults.Orders == options.Orders);
    }

    [Fact]
    public void Run_SameSeed_ProducesIdenticalMetrics()
    {
        var options = SmallOptions(seed: 99);
        var strategies = new[] { SchedulingStrategy.Capacity, SchedulingStrategy.Edd };

        var first = SimulationRunner.Run(options, strategies, FaultConfig.Default);
        var second = SimulationRunner.Run(options, strategies, FaultConfig.Default);

        first.OrderListHash.Should().Be(second.OrderListHash);
        first.Strategies.Should().BeEquivalentTo(second.Strategies);
    }
}
