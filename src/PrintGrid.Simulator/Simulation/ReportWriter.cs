using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PrintGrid.Simulator.Simulation;

/// <summary>Seed-stable text rendering for the CLI. No GUIDs, no wall-clock values.</summary>
public static class ReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string RenderGenerate(GeneratedNetwork network, SimulationOptions options)
    {
        var labIndex = network.Labs
            .Select((lab, index) => (lab.Id, Index: index))
            .ToDictionary(x => x.Id, x => x.Index);

        var document = new
        {
            command = "generate",
            seed = network.Seed,
            labs = network.Labs.Count,
            machines = network.Machines.Count,
            orders = network.Orders.Count,
            epochUtc = network.EpochUtc.ToString("O", CultureInfo.InvariantCulture),
            orderListHash = network.OrderListHash,
            labList = network.Labs.Select((lab, index) => new
            {
                index,
                name = lab.Name,
                city = lab.City,
                transitDaysToHub = lab.TransitDaysToHub,
                onTimeDeliveryRate = lab.OnTimeDeliveryRate,
                firstPassYield = lab.FirstPassYield,
                maxConcurrentJobs = lab.MaxConcurrentJobs
            }),
            machineList = network.Machines.Select((machine, index) => new
            {
                index,
                name = machine.Name,
                labIndex = labIndex[machine.LabId],
                technology = machine.Technology.ToString(),
                buildVolume = new
                {
                    widthMm = machine.BuildVolume.WidthMm,
                    depthMm = machine.BuildVolume.DepthMm,
                    heightMm = machine.BuildVolume.HeightMm
                },
                minLayerHeightMm = machine.MinLayerHeightMm,
                achievableToleranceMm = machine.AchievableToleranceMm,
                speedFactor = machine.SpeedFactor,
                supportedMaterials = machine.SupportedMaterials.OrderBy(x => x).ToArray()
            }),
            orderList = network.Orders.Select(order => new
            {
                index = order.Index,
                arrivalUtc = order.ArrivalUtc.ToString("O", CultureInfo.InvariantCulture),
                dueDate = order.Job.InternalDueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                technology = order.Profile.Technology,
                material = order.Profile.Material,
                color = order.Profile.Color,
                buildVolume = new
                {
                    widthMm = order.Profile.WidthMm,
                    depthMm = order.Profile.DepthMm,
                    heightMm = order.Profile.HeightMm
                },
                layerHeightMm = order.Profile.LayerHeightMm,
                toleranceMm = order.Profile.ToleranceMm,
                materialGrams = order.Profile.Grams,
                estimatedPrintMinutes = order.Profile.EstimatedMinutes,
                dueLeadDays = order.Profile.DueLeadDays,
                priority = order.Profile.Priority
            })
        };

        return JsonSerializer.Serialize(document, JsonOptions);
    }

    public static string RenderRun(SimulationReport report, FaultConfig faults)
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== PrintGrid NETWORK SIMULATION (UC-027 / RUN-04..06) ===");
        builder.AppendLine($"command: run");
        builder.AppendLine($"seed: {report.Seed}");
        builder.AppendLine($"network: labs={report.Labs} machines={report.Machines} orders={report.Orders}");
        builder.AppendLine($"order list hash: {report.OrderListHash}");
        builder.AppendLine($"faults: {faults}");
        builder.AppendLine($"elapsed: {report.ElapsedMs} ms (stress: {(report.Stress ? "yes" : "no")})");
        builder.AppendLine();

        var strategies = report.Strategies;
        if (strategies.Count == 0)
        {
            builder.AppendLine("No strategy selected.");
            return builder.ToString();
        }

        // AC01: one column per strategy.
        builder.AppendLine("Strategy comparison (faults enabled):");
        var header = "Metric".PadRight(30) + "|" + string.Join("|", strategies.Select(s => Center(s.WithFaults.Strategy, 12)));
        builder.AppendLine(header);
        builder.AppendLine(new string('-', header.Length));

        AppendRow(builder, "Completed / orders", strategies, m => $"{m.Completed}/{m.Orders}", 12);
        AppendRow(builder, "Failed", strategies, m => m.Failed.ToString(CultureInfo.InvariantCulture), 12);
        AppendRow(builder, "Weighted tardiness (min)", strategies, m => F1(m.WeightedTardinessMinutes), 12);
        AppendRow(builder, "On-time %", strategies, m => F1(m.OnTimePercent), 12);
        AppendRow(builder, "Utilisation %", strategies, m => F1(m.UtilizationPercent), 12);
        AppendRow(builder, "Changeovers", strategies, m => m.Changeovers.ToString(CultureInfo.InvariantCulture), 12);
        AppendRow(builder, "Gini load spread", strategies, m => m.GiniLoadSpread.ToString("0.0000", CultureInfo.InvariantCulture), 12);
        AppendRow(builder, "Reschedules (nervousness)", strategies, m => m.Reschedules.ToString(CultureInfo.InvariantCulture), 12);
        AppendRow(builder, "Estimate MAPE %", strategies, m => F1(m.EstimateMapePercent), 12);
        AppendRow(builder, "Makespan (days)", strategies, m => F1(m.MakespanDays), 12);
        AppendRow(builder, "Declines (fault a)", strategies, m => m.Declines.ToString(CultureInfo.InvariantCulture), 12);
        AppendRow(builder, "Print failures (fault b)", strategies, m => m.PrintFailures.ToString(CultureInfo.InvariantCulture), 12);
        AppendRow(builder, "Breakdowns (fault c)", strategies, m => m.Breakdowns.ToString(CultureInfo.InvariantCulture), 12);
        AppendRow(builder, "Inspection fails (fault d)", strategies, m => m.InspectionFailures.ToString(CultureInfo.InvariantCulture), 12);
        AppendRow(builder, "Downtime (min)", strategies, m => F1(m.DowntimeMinutes), 12);
        builder.AppendLine();

        // Per-strategy fault impact against the no-fault baseline.
        builder.AppendLine("Fault impact (baseline -> with faults):");
        var impactHeader = "Strategy".PadRight(12)
            + "| " + "On-time %".PadRight(16)
            + "| " + "Tardiness (min)".PadRight(20)
            + "| " + "Reschedules".PadRight(14)
            + "| Faults D/P/B/I";
        builder.AppendLine(impactHeader);
        builder.AppendLine(new string('-', impactHeader.Length));

        foreach (var comparison in strategies)
        {
            var baseline = comparison.Baseline;
            var faulted = comparison.WithFaults;
            var faultTotal = faulted.Declines + faulted.PrintFailures + faulted.Breakdowns + faulted.InspectionFailures;
            builder.AppendLine(
                faulted.Strategy.PadRight(12)
                + "| " + $"{F1(baseline.OnTimePercent)} -> {F1(faulted.OnTimePercent)}".PadRight(16)
                + "| " + $"{F1(baseline.WeightedTardinessMinutes)} -> {F1(faulted.WeightedTardinessMinutes)}".PadRight(20)
                + "| " + $"{baseline.Reschedules} -> {faulted.Reschedules}".PadRight(14)
                + "| " + $"{faulted.Declines}/{faulted.PrintFailures}/{faulted.Breakdowns}/{faulted.InspectionFailures} (total {faultTotal})");
        }

        builder.AppendLine();
        builder.AppendLine("Ran with the virtual clock; same seed + same flags reproduce these metrics.");

        if (report.Stress)
        {
            builder.AppendLine($"STRESS MODE: 50/300/1000 preset, completed in {report.ElapsedMs} ms.");
            builder.AppendLine($"30s wall-clock target: {(report.ElapsedMs <= 30_000 ? "PASS" : "EXCEEDED")}.");
        }

        return builder.ToString();
    }

    private static void AppendRow(
        StringBuilder builder,
        string label,
        IReadOnlyList<StrategyComparison> strategies,
        Func<SimulationMetrics, string> value,
        int columnWidth)
    {
        var cells = strategies.Select(s => Center(value(s.WithFaults), columnWidth));
        builder.AppendLine(label.PadRight(30) + "|" + string.Join("|", cells));
    }

    private static string Center(string text, int width) =>
        text.Length >= width ? text : text.PadLeft((width + text.Length) / 2).PadRight(width);

    private static string F1(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
}
