using System.Globalization;
using System.Text.Json;
using PrintGrid.Simulator.Simulation;

namespace PrintGrid.Simulator;

public static class Cli
{
    private static readonly SchedulingStrategy[] AllStrategies =
        [SchedulingStrategy.Capacity, SchedulingStrategy.Edd, SchedulingStrategy.Nearest, SchedulingStrategy.Random];

    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].Trim().ToLowerInvariant();
        var options = new SimulationOptions();
        var faults = FaultConfig.Default;
        var disableFaults = false;
        IReadOnlyList<SchedulingStrategy> strategies = AllStrategies;
        var assertDeterministic = false;
        var labsExplicit = false;
        var machinesExplicit = false;
        var ordersExplicit = false;
        var stress = false;

        try
        {
            for (var i = 1; i < args.Length; i++)
            {
                var flag = args[i];
                switch (flag)
                {
                    case "--labs":
                        options.Labs = NextInt(args, ref i, flag);
                        labsExplicit = true;
                        break;
                    case "--machines":
                        options.Machines = NextInt(args, ref i, flag);
                        machinesExplicit = true;
                        break;
                    case "--orders":
                        options.Orders = NextInt(args, ref i, flag);
                        ordersExplicit = true;
                        break;
                    case "--seed":
                        options.Seed = NextInt(args, ref i, flag);
                        break;
                    case "--strategy":
                        strategies = ParseStrategies(NextValue(args, ref i, flag));
                        break;
                    case "--faults":
                        faults = ParseFaults(NextValue(args, ref i, flag), faults);
                        break;
                    case "--decline-rate":
                        faults = faults.With(declineRate: NextDouble(args, ref i, flag));
                        break;
                    case "--print-failure-rate":
                        faults = faults.With(printFailureRate: NextDouble(args, ref i, flag));
                        break;
                    case "--breakdown-rate":
                        faults = faults.With(breakdownRate: NextDouble(args, ref i, flag));
                        break;
                    case "--mean-downtime-minutes":
                        faults = faults.With(meanDowntimeMinutes: NextDouble(args, ref i, flag));
                        break;
                    case "--inspection-fail-rate":
                        faults = faults.With(inspectionFailRate: NextDouble(args, ref i, flag));
                        break;
                    case "--no-faults":
                        disableFaults = true;
                        break;
                    case "--stress":
                        stress = true;
                        break;
                    case "--assert-deterministic":
                        assertDeterministic = true;
                        break;
                    case "--help":
                    case "-h":
                        PrintUsage();
                        return 0;
                    default:
                        throw new ArgumentException($"Unknown option: {flag}");
                }
            }

            if (stress)
            {
                var preset = SimulationOptions.StressPreset(options.Seed);
                if (!labsExplicit) options.Labs = preset.Labs;
                if (!machinesExplicit) options.Machines = preset.Machines;
                if (!ordersExplicit) options.Orders = preset.Orders;
                options.Stress = true;
            }

            if (disableFaults) faults = faults.DisableAll();

            options.Validate();
            faults.Validate();

            return command switch
            {
                "generate" => RunGenerate(options, assertDeterministic),
                "run" => RunSimulation(options, strategies, faults),
                _ => UnknownCommand(command)
            };
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static int RunGenerate(SimulationOptions options, bool assertDeterministic)
    {
        var network = NetworkGenerator.Generate(options);
        var rendered = ReportWriter.RenderGenerate(network, options);

        if (assertDeterministic)
        {
            var second = NetworkGenerator.Generate(options);
            var secondRendered = ReportWriter.RenderGenerate(second, options);
            if (!string.Equals(rendered, secondRendered, StringComparison.Ordinal)
                || !string.Equals(network.OrderListHash, second.OrderListHash, StringComparison.Ordinal))
            {
                Console.Error.WriteLine("deterministic self-check: FAIL (same seed produced different output)");
                return 2;
            }

            Console.Error.WriteLine("deterministic self-check: PASS (two generations with the same seed are byte-identical)");
        }

        Console.WriteLine(rendered);
        return 0;
    }

    private static int RunSimulation(
        SimulationOptions options,
        IReadOnlyList<SchedulingStrategy> strategies,
        FaultConfig faults)
    {
        var report = SimulationRunner.Run(options, strategies, faults);
        Console.WriteLine(ReportWriter.RenderRun(report, faults));
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"error: unknown command '{command}'");
        PrintUsage();
        return 1;
    }

    private static IReadOnlyList<SchedulingStrategy> ParseStrategies(string value)
    {
        if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase))
            return AllStrategies;

        if (!Enum.TryParse<SchedulingStrategy>(value, ignoreCase: true, out var strategy))
            throw new ArgumentException($"Unknown strategy '{value}'. Use capacity|edd|nearest|random|all.");

        return [strategy];
    }

    private static FaultConfig ParseFaults(string value, FaultConfig current)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith('{'))
            return ParseJsonFaults(trimmed, current);

        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("--faults requires a JSON object or key=value pairs");

        foreach (var part in trimmed.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
                throw new ArgumentException($"Invalid fault pair '{part}'. Use key=value.");

            var key = part[..separator].Trim().ToLowerInvariant();
            var raw = part[(separator + 1)..].Trim();
            var parsed = double.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture);
            current = ApplyFaultPair(current, key, parsed);
        }

        return current;
    }

    private static FaultConfig ParseJsonFaults(string json, FaultConfig current)
    {
        using var document = JsonDocument.Parse(json);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Number)
                throw new ArgumentException($"Fault '{property.Name}' must be numeric");

            current = ApplyFaultPair(current, property.Name.ToLowerInvariant(), property.Value.GetDouble());
        }

        return current;
    }

    private static FaultConfig ApplyFaultPair(FaultConfig current, string key, double parsed) => key switch
    {
        "decline" or "declinerate" => current.With(declineRate: parsed),
        "print" or "printfailurerate" or "printfailure" => current.With(printFailureRate: parsed),
        "breakdown" or "breakdownrate" => current.With(breakdownRate: parsed),
        "downtime" or "meandowntimeminutes" or "meandowntime" => current.With(meanDowntimeMinutes: parsed),
        "inspection" or "inspectionfailrate" or "inspectionfail" => current.With(inspectionFailRate: parsed),
        "enabled" => parsed == 0 ? current.DisableAll() : current.With(),
        _ => throw new ArgumentException($"Unknown fault key '{key}'")
    };

    private static string NextValue(string[] args, ref int index, string flag)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"{flag} requires a value");
        return args[++index];
    }

    private static int NextInt(string[] args, ref int index, string flag) =>
        int.Parse(NextValue(args, ref index, flag), NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static double NextDouble(string[] args, ref int index, string flag) =>
        double.Parse(NextValue(args, ref index, flag), NumberStyles.Float, CultureInfo.InvariantCulture);

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            PrintGrid network simulation (UC-027 / RUN-04..06)

            Usage:
              dotnet run --project src/PrintGrid.Simulator -- <command> [options]

            Commands:
              generate  Build a seed-stable virtual network and print its order list + SHA-256 hash.
              run       Schedule the network with one or all strategies, inject faults, repair and report.

            Options:
              --seed <int>                   Seed for every random stream (default 42).
              --labs <n> --machines <m> --orders <k>
              --strategy <capacity|edd|nearest|random|all>   Default: all (4-column AC01 table).
              --faults <json|key=value,...>  e.g. {"declineRate":0.1,"printFailureRate":0.05,
                                              "breakdownRate":0.02,"meanDowntimeMinutes":120,
                                              "inspectionFailRate":0.03}
                                              or decline=0.1,print=0.05,breakdown=0.02,downtime=120,inspection=0.03
              --decline-rate <0..1>
              --print-failure-rate <0..1>
              --breakdown-rate <0..1>
              --mean-downtime-minutes <n>
              --inspection-fail-rate <0..1>
              --no-faults                    Run the fault-free baseline only.
              --stress                       Use 50 labs / 300 machines / 1000 orders (unless overridden).
              --assert-deterministic         generate only: run twice and fail on any difference.

            Determinism: same seed + same flags => same generated orders, same order hash and same
            simulated metrics (all time comes from the virtual clock; no DateTime.UtcNow in the sim core).
            """);
    }
}
