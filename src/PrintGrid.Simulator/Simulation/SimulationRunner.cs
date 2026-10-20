using System.Diagnostics;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Simulator.Simulation;

/// <summary>One row of the AC01 strategy comparison.</summary>
public sealed record SimulationMetrics(
    string Strategy,
    int Orders,
    int Completed,
    int Failed,
    double WeightedTardinessMinutes,
    double OnTimePercent,
    double UtilizationPercent,
    int Changeovers,
    double GiniLoadSpread,
    int Reschedules,
    double EstimateMapePercent,
    int Declines,
    int PrintFailures,
    int Breakdowns,
    int InspectionFailures,
    double DowntimeMinutes,
    double MakespanDays);

/// <summary>A strategy's no-fault baseline next to its run with faults enabled.</summary>
public sealed record StrategyComparison(
    SimulationMetrics Baseline,
    SimulationMetrics WithFaults,
    FaultConfig Faults);

public sealed record SimulationReport(
    int Seed,
    int Labs,
    int Machines,
    int Orders,
    string OrderListHash,
    IReadOnlyList<StrategyComparison> Strategies,
    bool Stress,
    long ElapsedMs);

/// <summary>
/// RUN-05/RUN-06: batch scheduling through <see cref="StrategyAssigner"/> followed by a
/// discrete-event execution loop on a <see cref="VirtualClock"/>. A fault immediately runs the
/// assigner again for the affected job ("hỏng ⇒ vá lịch bắt buộc chạy") and counts as one
/// reschedule (nervousness).
/// </summary>
public static class SimulationRunner
{
    private const int MaxOffersPerPlacement = 6;
    private const int MaxReschedulesPerJob = 6;

    // Distinct, process-stable salts. These are plain integers (never HashCode.Combine or
    // Random.Shared) so every random stream is reproducible across processes.
    private const int DeclineSalt = 0x1A2B3C4D;
    private const int PrintSalt = 0x2B3C4D5E;
    private const int BreakdownSalt = 0x3C4D5E6F;
    private const int InspectionSalt = 0x4D5E6F70;
    private const int NoiseSalt = 0x5E6F7081;
    private const int StrategySalt = 0x6F708192;

    public static SimulationReport Run(
        SimulationOptions options,
        IReadOnlyList<SchedulingStrategy> strategies,
        FaultConfig faults)
    {
        options.Validate();
        faults.Validate();

        var stopwatch = Stopwatch.StartNew();
        var comparisons = new List<StrategyComparison>(strategies.Count);
        var orderHash = string.Empty;

        foreach (var strategy in strategies)
        {
            // Two fresh networks with the same seed: the baseline proves what the strategy does
            // without faults, the faulted run shows the repair behaviour.
            var baselineNetwork = NetworkGenerator.Generate(options);
            orderHash = baselineNetwork.OrderListHash;
            var baseline = Execute(baselineNetwork, strategy, FaultConfig.None);

            var faultedNetwork = NetworkGenerator.Generate(options);
            var withFaults = Execute(faultedNetwork, strategy, faults);

            comparisons.Add(new StrategyComparison(baseline, withFaults, faults));
        }

        stopwatch.Stop();
        return new SimulationReport(
            options.Seed,
            options.Labs,
            options.Machines,
            options.Orders,
            orderHash,
            comparisons,
            options.Stress,
            stopwatch.ElapsedMilliseconds);
    }

    private static SimulationMetrics Execute(GeneratedNetwork network, SchedulingStrategy strategy, FaultConfig faults)
    {
        var engine = new SimulationRun(network, strategy, faults);
        engine.Execute();
        return engine.BuildMetrics(strategy.ToString().ToLowerInvariant());
    }

    private enum EventKind
    {
        PrintStart,
        PrintDone,
        PrintFault,
        Inspection
    }

    private sealed record ScheduledEvent(
        DateTime AtUtc,
        long Sequence,
        EventKind Kind,
        int OrderIndex,
        int Token,
        string? FaultKind = null,
        double DowntimeMinutes = 0);

    private sealed class OrderState
    {
        public int Token;
        public Guid MachineId;
        public Guid LabId;
        public DateTime BookingStartUtc;
        public DateTime BookingEndUtc;
        public DateTime? PrintedAtUtc;
        public DateTime? CompletedAtUtc;
        public int ActualPrintMinutes;
        public double BusyMinutes;
        public int Reschedules;
        public bool GaveUp;
    }

    private sealed record PlacementRecord(
        Guid MachineId,
        Guid LabId,
        DateTime StartUtc,
        string Material,
        string Color);

    private sealed class SimulationRun
    {
        private static readonly Guid SimStaffId = new("11111111-1111-1111-1111-111111111111");

        private readonly GeneratedNetwork _network;
        private readonly SchedulingStrategy _strategy;
        private readonly FaultConfig _faults;
        private readonly VirtualClock _clock;
        private readonly MachineCalendar _calendar = new();
        private readonly StrategyAssigner _assigner;
        private readonly Dictionary<Guid, Machine> _machinesById;
        private readonly Dictionary<Guid, Lab> _labsById;

        private readonly PriorityQueue<ScheduledEvent, (long Ticks, long Sequence)> _queue = new();
        private readonly OrderState[] _states;
        private readonly List<PlacementRecord> _placements = [];
        private readonly Dictionary<Guid, double> _labBusyMinutes = [];
        private long _sequence;

        private readonly Random _strategyRandom;
        private readonly Random _declineRandom;
        private readonly Random _printRandom;
        private readonly Random _breakdownRandom;
        private readonly Random _inspectionRandom;
        private readonly Random _noiseRandom;

        private int _declines;
        private int _printFailures;
        private int _breakdowns;
        private int _inspectionFailures;
        private int _reschedules;
        private int _unplaced;
        private double _downtimeMinutes;
        private DateTime _lastActivityUtc;

        public SimulationRun(GeneratedNetwork network, SchedulingStrategy strategy, FaultConfig faults)
        {
            _network = network;
            _strategy = strategy;
            _faults = faults;
            _clock = new VirtualClock(network.EpochUtc);
            _assigner = new StrategyAssigner(network.Labs, _calendar);
            _machinesById = network.Machines.ToDictionary(m => m.Id);
            _labsById = network.Labs.ToDictionary(l => l.Id);
            _states = network.Orders.Select(_ => new OrderState()).ToArray();
            _lastActivityUtc = network.EpochUtc;

            _strategyRandom = new Random(StreamSeed(network.Seed, StrategySalt + (int)strategy * 7919));
            _declineRandom = new Random(StreamSeed(network.Seed, DeclineSalt));
            _printRandom = new Random(StreamSeed(network.Seed, PrintSalt));
            _breakdownRandom = new Random(StreamSeed(network.Seed, BreakdownSalt));
            _inspectionRandom = new Random(StreamSeed(network.Seed, InspectionSalt));
            _noiseRandom = new Random(StreamSeed(network.Seed, NoiseSalt));
        }

        public void Execute()
        {
            var ordered = OrderForStrategy();

            // Batch placement: all orders are known up front, so EDD can order the whole queue.
            foreach (var order in ordered)
            {
                var notBefore = order.ArrivalUtc > _clock.UtcNow ? order.ArrivalUtc : _clock.UtcNow;
                if (!Place(order.Index, notBefore, excludeLabId: null))
                {
                    _unplaced++;
                    _states[order.Index].GaveUp = true;
                }
            }

            while (_queue.TryDequeue(out var scheduled, out _))
            {
                _clock.AdvanceTo(scheduled.AtUtc);
                switch (scheduled.Kind)
                {
                    case EventKind.PrintStart:
                        HandlePrintStart(scheduled);
                        break;
                    case EventKind.PrintDone:
                        HandlePrintDone(scheduled);
                        break;
                    case EventKind.PrintFault:
                        HandlePrintFault(scheduled);
                        break;
                    case EventKind.Inspection:
                        HandleInspection(scheduled);
                        break;
                }
            }
        }

        private List<SimOrder> OrderForStrategy()
        {
            var orders = _network.Orders.ToList();
            switch (_strategy)
            {
                case SchedulingStrategy.Edd:
                    return orders
                        .OrderBy(o => o.Job.InternalDueDate)
                        .ThenBy(o => o.ArrivalUtc)
                        .ThenBy(o => o.Index)
                        .ToList();
                case SchedulingStrategy.Random:
                    // Fisher-Yates from the strategy stream: deterministic for a given seed.
                    for (var i = orders.Count - 1; i > 0; i--)
                    {
                        var j = _strategyRandom.Next(i + 1);
                        (orders[i], orders[j]) = (orders[j], orders[i]);
                    }

                    return orders;
                default:
                    return orders.OrderBy(o => o.ArrivalUtc).ThenBy(o => o.Index).ToList();
            }
        }

        private bool Place(int orderIndex, DateTime notBeforeUtc, Guid? excludeLabId)
        {
            var order = _network.Orders[orderIndex];
            var state = _states[orderIndex];

            if (state.GaveUp) return false;

            for (var offer = 0; offer < MaxOffersPerPlacement; offer++)
            {
                var proposal = _assigner.TryAssign(
                    order.Job, notBeforeUtc, _strategy, _strategyRandom, excludeLabId);
                if (proposal is null) return false;

                var assignment = order.Job.AssignTo(
                    proposal.LabId,
                    proposal.MachineId,
                    proposal.StartUtc,
                    proposal.EndUtc,
                    proposal.Score,
                    _clock.UtcNow);
                if (assignment.IsFailure)
                {
                    _calendar.RemoveBooking(proposal.MachineId, proposal.StartUtc, proposal.EndUtc);
                    return false;
                }

                // Fault (a): the lab declines the offer, the slot is released and the job is
                // offered again while excluding the declining lab (FR-SCHED-007).
                var declineRoll = _declineRandom.NextDouble();
                if (_faults.DeclineEnabled && declineRoll < _faults.DeclineRate)
                {
                    _declines++;
                    _reschedules++;
                    state.Reschedules++;
                    _calendar.RemoveBooking(proposal.MachineId, proposal.StartUtc, proposal.EndUtc);
                    order.Job.Decline("Simulated lab decline");
                    excludeLabId = proposal.LabId;
                    continue;
                }

                var acceptance = order.Job.Accept(_clock.UtcNow);
                if (acceptance.IsFailure)
                {
                    _calendar.RemoveBooking(proposal.MachineId, proposal.StartUtc, proposal.EndUtc);
                    return false;
                }

                state.Token++;
                state.MachineId = proposal.MachineId;
                state.LabId = proposal.LabId;
                state.BookingStartUtc = proposal.StartUtc;
                state.BookingEndUtc = proposal.EndUtc;
                _placements.Add(new PlacementRecord(
                    proposal.MachineId,
                    proposal.LabId,
                    proposal.StartUtc,
                    order.Job.Specification.MaterialCode,
                    order.Job.Specification.ColorCode));

                if (proposal.EndUtc > _lastActivityUtc) _lastActivityUtc = proposal.EndUtc;

                Enqueue(new ScheduledEvent(
                    proposal.StartUtc, _sequence++, EventKind.PrintStart, orderIndex, state.Token));
                return true;
            }

            return false;
        }

        private void HandlePrintStart(ScheduledEvent scheduled)
        {
            var state = _states[scheduled.OrderIndex];
            if (state.Token != scheduled.Token) return;

            var order = _network.Orders[scheduled.OrderIndex];
            if (order.Job.Status != JobStatus.Accepted) return;

            var downUntil = _calendar.DowntimeEndAt(state.MachineId, scheduled.AtUtc);
            if (downUntil is { } repairEnd && repairEnd > scheduled.AtUtc)
            {
                // A breakdown from an earlier job pushed into this slot: re-plan from repair end.
                if (state.Reschedules >= MaxReschedulesPerJob)
                {
                    state.GaveUp = true;
                    return;
                }

                if (order.Job.PrepareForOpsReassignment("Machine downtime requires re-planning").IsFailure)
                    return;

                state.Reschedules++;
                _reschedules++;
                _states[scheduled.OrderIndex].Token++; // invalidate this attempt's events
                if (!Place(scheduled.OrderIndex, repairEnd, excludeLabId: null))
                {
                    state.GaveUp = true;
                    _unplaced++;
                }

                return;
            }

            if (order.Job.Start(scheduled.AtUtc).IsFailure) return;

            var duration = (state.BookingEndUtc - state.BookingStartUtc).TotalMinutes;

            // Fault draws happen whether or not the fault is enabled, so each type's stream is
            // independent of which other faults are switched on.
            var printRoll = _printRandom.NextDouble();
            var breakdownRoll = _breakdownRandom.NextDouble();

            // Fault (b): print failure at 60-90% of the duration.
            if (_faults.PrintFailureEnabled && printRoll < _faults.PrintFailureRate)
            {
                var fraction = 0.60 + _printRandom.NextDouble() * 0.30;
                var faultAt = scheduled.AtUtc.AddMinutes(duration * fraction);
                Enqueue(new ScheduledEvent(
                    faultAt, _sequence++, EventKind.PrintFault, scheduled.OrderIndex, state.Token,
                    FaultKind: "print_failure"));
            }

            // Fault (c): machine breakdown at 10-90% of the duration, exponential downtime.
            if (_faults.BreakdownEnabled && breakdownRoll < _faults.BreakdownRate)
            {
                var fraction = 0.10 + _breakdownRandom.NextDouble() * 0.80;
                var mean = Math.Max(1.0, _faults.MeanDowntimeMinutes);
                var downtime = -Math.Log(1.0 - _breakdownRandom.NextDouble()) * mean;
                var faultAt = scheduled.AtUtc.AddMinutes(duration * fraction);
                Enqueue(new ScheduledEvent(
                    faultAt, _sequence++, EventKind.PrintFault, scheduled.OrderIndex, state.Token,
                    FaultKind: "breakdown", DowntimeMinutes: downtime));
            }

            Enqueue(new ScheduledEvent(
                state.BookingEndUtc, _sequence++, EventKind.PrintDone, scheduled.OrderIndex, state.Token));
        }

        private void HandlePrintFault(ScheduledEvent scheduled)
        {
            var state = _states[scheduled.OrderIndex];
            if (state.Token != scheduled.Token) return;

            var order = _network.Orders[scheduled.OrderIndex];
            if (order.Job.Status != JobStatus.InProgress) return;

            var elapsedMinutes = Math.Max(0.0, (scheduled.AtUtc - state.BookingStartUtc).TotalMinutes);
            state.BusyMinutes += elapsedMinutes;
            AddLabBusy(state.LabId, elapsedMinutes);
            _calendar.TrimBooking(state.MachineId, state.BookingStartUtc, state.BookingEndUtc, scheduled.AtUtc);

            if (scheduled.FaultKind == "breakdown")
            {
                _breakdowns++;
                var downtime = Math.Max(1.0, scheduled.DowntimeMinutes);
                _downtimeMinutes += downtime;
                _calendar.Book(state.MachineId, scheduled.AtUtc, scheduled.AtUtc.AddMinutes(downtime), isDowntime: true);
                order.Job.Fail("Simulated machine breakdown");
            }
            else
            {
                _printFailures++;
                order.Job.Fail("Simulated print failure");
            }

            state.Reschedules++;
            _reschedules++;
            if (state.Reschedules > MaxReschedulesPerJob)
            {
                state.GaveUp = true;
                return;
            }

            order.Job.PrepareForReassignment();
            if (!Place(scheduled.OrderIndex, scheduled.AtUtc, excludeLabId: null))
            {
                state.GaveUp = true;
                _unplaced++;
            }
        }

        private void HandlePrintDone(ScheduledEvent scheduled)
        {
            var state = _states[scheduled.OrderIndex];
            if (state.Token != scheduled.Token) return;

            var order = _network.Orders[scheduled.OrderIndex];
            if (order.Job.Status != JobStatus.InProgress) return;

            var estimated = order.Job.EstimatedPrintMinutes;
            var noise = (_noiseRandom.NextDouble() - 0.5) * 0.10; // +/-5%
            var actualMinutes = Math.Max(1, (int)Math.Round(estimated * (1.0 + noise)));
            var gramNoise = (_noiseRandom.NextDouble() - 0.5) * 0.10;
            var actualGrams = Math.Max(1m, decimal.Round(order.Job.Specification.MaterialGrams * (decimal)(1.0 + gramNoise), 1));

            var completion = order.Job.Complete(
                scheduled.AtUtc,
                actualMinutes,
                "Simulated lab completion",
                ["sim/proof-1"],
                actualGrams);
            if (completion.IsFailure) return;

            var review = order.Job.ReviewQcProof(true, SimStaffId, null, scheduled.AtUtc);
            if (review.IsFailure) return;

            var elapsedMinutes = Math.Max(0.0, (scheduled.AtUtc - state.BookingStartUtc).TotalMinutes);
            state.BusyMinutes += elapsedMinutes;
            AddLabBusy(state.LabId, elapsedMinutes);
            state.PrintedAtUtc = scheduled.AtUtc;
            state.ActualPrintMinutes = actualMinutes;

            if (scheduled.AtUtc > _lastActivityUtc) _lastActivityUtc = scheduled.AtUtc;

            // Transfer to the hub takes the lab's transit days, then inspection runs.
            var lab = _labsById[state.LabId];
            var inspectionAt = scheduled.AtUtc.AddDays(lab.TransitDaysToHub).AddHours(2);
            Enqueue(new ScheduledEvent(
                inspectionAt, _sequence++, EventKind.Inspection, scheduled.OrderIndex, state.Token));
        }

        private void HandleInspection(ScheduledEvent scheduled)
        {
            var state = _states[scheduled.OrderIndex];
            if (state.Token != scheduled.Token) return;

            var order = _network.Orders[scheduled.OrderIndex];
            if (order.Job.Status != JobStatus.AwaitingInspection) return;

            // Fault (d): hub inspection rejects the printed part.
            var inspectionRoll = _inspectionRandom.NextDouble();
            if (_faults.InspectionFailEnabled && inspectionRoll < _faults.InspectionFailRate)
            {
                _inspectionFailures++;
                order.Job.Fail("Simulated hub inspection failure");

                state.Reschedules++;
                _reschedules++;
                if (state.Reschedules > MaxReschedulesPerJob)
                {
                    state.GaveUp = true;
                    return;
                }

                order.Job.PrepareForReassignment();
                if (!Place(scheduled.OrderIndex, scheduled.AtUtc, excludeLabId: null))
                {
                    state.GaveUp = true;
                    _unplaced++;
                }

                return;
            }

            if (order.Job.MarkInspectionPassed(["sim/proof-1"]).IsFailure) return;
            state.CompletedAtUtc = scheduled.AtUtc;
            if (scheduled.AtUtc > _lastActivityUtc) _lastActivityUtc = scheduled.AtUtc;
        }

        public SimulationMetrics BuildMetrics(string strategyName)
        {
            var orders = _network.Orders;
            var completed = 0;
            var onTime = 0;
            var weightedTardiness = 0.0;
            var mapeSum = 0.0;

            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var state = _states[i];
                if (state.CompletedAtUtc is not { } completedAt) continue;

                completed++;
                var dueUtc = order.Job.InternalDueDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                var tardiness = Math.Max(0.0, (completedAt - dueUtc).TotalMinutes);
                var weight = order.Job.Priority == JobPriority.Urgent ? 3.0 : 1.0;
                weightedTardiness += tardiness * weight;
                if (tardiness <= 0) onTime++;

                if (state.ActualPrintMinutes > 0)
                {
                    mapeSum += Math.Abs(state.ActualPrintMinutes - order.Job.EstimatedPrintMinutes)
                        / (double)order.Job.EstimatedPrintMinutes;
                }
            }

            var makespanMinutes = Math.Max(1.0, (_lastActivityUtc - _network.EpochUtc).TotalMinutes);
            var totalBusy = _states.Sum(s => s.BusyMinutes);
            var utilization = Math.Clamp(totalBusy / (makespanMinutes * _network.Machines.Count), 0.0, 1.0) * 100.0;

            var gini = ComputeGini(_network.Labs
                .Select(lab => _labBusyMinutes.TryGetValue(lab.Id, out var minutes) ? minutes : 0.0)
                .ToList());

            return new SimulationMetrics(
                Strategy: strategyName,
                Orders: orders.Count,
                Completed: completed,
                Failed: orders.Count - completed,
                WeightedTardinessMinutes: Math.Round(weightedTardiness, 1),
                OnTimePercent: orders.Count == 0 ? 0 : Math.Round(100.0 * onTime / orders.Count, 1),
                UtilizationPercent: Math.Round(utilization, 1),
                Changeovers: CountChangeovers(),
                GiniLoadSpread: Math.Round(gini, 4),
                Reschedules: _reschedules,
                EstimateMapePercent: completed == 0 ? 0 : Math.Round(100.0 * mapeSum / completed, 2),
                Declines: _declines,
                PrintFailures: _printFailures,
                Breakdowns: _breakdowns,
                InspectionFailures: _inspectionFailures,
                DowntimeMinutes: Math.Round(_downtimeMinutes, 1),
                MakespanDays: Math.Round(makespanMinutes / (24 * 60), 2));
        }

        private int CountChangeovers()
        {
            var total = 0;
            foreach (var machine in _placements.GroupBy(p => p.MachineId))
            {
                var sequence = machine.OrderBy(p => p.StartUtc).ToList();
                for (var i = 1; i < sequence.Count; i++)
                {
                    if (!string.Equals(sequence[i].Material, sequence[i - 1].Material, StringComparison.Ordinal)
                        || !string.Equals(sequence[i].Color, sequence[i - 1].Color, StringComparison.Ordinal))
                    {
                        total++;
                    }
                }
            }

            return total;
        }

        private void AddLabBusy(Guid labId, double minutes)
        {
            if (minutes <= 0) return;
            _labBusyMinutes[labId] = _labBusyMinutes.TryGetValue(labId, out var existing)
                ? existing + minutes
                : minutes;
        }

        private void Enqueue(ScheduledEvent scheduled) =>
            _queue.Enqueue(scheduled, (scheduled.AtUtc.Ticks, scheduled.Sequence));

        private static int StreamSeed(int seed, int salt) => unchecked(seed * 397 ^ salt);

        /// <summary>Gini coefficient over per-lab busy minutes (0 = perfectly even load).</summary>
        private static double ComputeGini(IReadOnlyList<double> values)
        {
            var sorted = values.OrderBy(v => v).ToArray();
            var count = sorted.Length;
            var sum = sorted.Sum();
            if (count == 0 || sum <= 0) return 0;

            var cumulative = 0.0;
            for (var i = 0; i < count; i++) cumulative += (i + 1) * sorted[i];
            return (2 * cumulative) / (count * sum) - (count + 1.0) / count;
        }
    }
}
