using System.Security.Cryptography;
using System.Text;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;

namespace PrintGrid.Simulator.Simulation;

/// <summary>
/// RUN-04: generates labs, machines and Poisson-ish orders from ONE <see cref="Random"/> seeded
/// with <see cref="SimulationOptions.Seed"/>. All draws happen in a fixed order, so the same
/// seed always yields the same network and the same <see cref="GeneratedNetwork.OrderListHash"/>.
/// GUIDs created by the domain are deliberately excluded from the hash and from all output.
/// </summary>
public static class NetworkGenerator
{
    public static readonly DateTime EpochUtc = new(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc);

    private static readonly string[] Cities =
        ["Hanoi", "Ho Chi Minh City", "Da Nang", "Can Tho", "Hai Phong", "Hue", "Nha Trang", "Vung Tau"];

    private static readonly string[] Colors = ["BLACK", "WHITE", "GREY", "RED", "BLUE"];

    private static readonly IReadOnlyDictionary<PrintTechnology, string[]> MaterialsByTechnology =
        new Dictionary<PrintTechnology, string[]>
        {
            [PrintTechnology.Fdm] = ["PLA", "PETG", "ABS"],
            [PrintTechnology.Sla] = ["RESIN-STD", "RESIN-TOUGH"],
            [PrintTechnology.Sls] = ["NYLON-PA12"]
        };

    public static GeneratedNetwork Generate(SimulationOptions options)
    {
        options.Validate();
        var random = new Random(options.Seed);

        var labs = GenerateLabs(options.Labs, random);
        var machines = GenerateMachines(options.Machines, labs, random);
        SeedMaterialStock(labs);
        var orders = GenerateOrders(options.Orders, machines, random);

        return new GeneratedNetwork
        {
            Seed = options.Seed,
            EpochUtc = EpochUtc,
            Labs = labs,
            Machines = machines,
            Orders = orders,
            OrderListHash = ComputeOrderListHash(orders)
        };
    }

    private static List<Lab> GenerateLabs(int count, Random random)
    {
        var labs = new List<Lab>(count);
        for (var i = 0; i < count; i++)
        {
            var city = Cities[random.Next(Cities.Length)];
            // Reliability / transit / speed are random but seed-stable (RUN-04).
            var transitDays = random.Next(0, 4);
            var onTime = 0.70m + (decimal)random.NextDouble() * 0.29m;
            var firstPassYield = 0.80m + (decimal)random.NextDouble() * 0.19m;

            var lab = Lab.Onboard(
                name: $"Lab-{i + 1:000}",
                city: city,
                transitDaysToHub: transitDays,
                workingDays: "Monday,Tuesday,Wednesday,Thursday,Friday",
                openingTime: "08:00",
                closingTime: "18:00");

            lab.UpdatePerformance(decimal.Round(onTime, 3), decimal.Round(firstPassYield, 3));
            labs.Add(lab);
        }

        return labs;
    }

    private static List<Machine> GenerateMachines(int count, List<Lab> labs, Random random)
    {
        var machines = new List<Machine>(count);

        for (var i = 0; i < count; i++)
        {
            // Round-robin over labs so every lab has at least one machine when machines >= labs.
            var lab = labs[i % labs.Count];
            var technology = i < 3
                ? (PrintTechnology)(i % 3) // guarantee at least one machine per technology
                : DrawTechnology(random, [PrintTechnology.Fdm, PrintTechnology.Sla, PrintTechnology.Sls]);

            var (minLayer, _) = LayerRange(technology);
            // The three guaranteed machines (one per technology) use the tightest tolerance so
            // every generated order has at least one capable machine of its technology.
            var tolerance = i < 3
                ? 0.10m
                : 0.10m + (decimal)random.NextDouble() * 0.10m;
            var volumeBase = 250m + (decimal)random.NextDouble() * 150m;

            var buildVolume = BuildVolume.Create(
                volumeBase,
                volumeBase + (decimal)random.NextDouble() * 80m,
                volumeBase + (decimal)random.NextDouble() * 80m);

            var speedFactor = 0.60m + (decimal)random.NextDouble() * 0.90m; // 0.60 .. 1.50
            if (speedFactor > 1.49m) speedFactor = 1.49m;

            var machine = Machine.Register(
                lab.Id,
                $"M-{i + 1:000}",
                $"{technology}-{200 + random.Next(4) * 100}",
                technology,
                buildVolume,
                minLayer,
                decimal.Round(tolerance, 2),
                MaterialsByTechnology[technology]);

            machine.AdjustSpeedFactor(decimal.Round(speedFactor, 3));
            lab.AddMachine(machine);
            machines.Add(machine);
        }

        return machines;
    }

    private static void SeedMaterialStock(List<Lab> labs)
    {
        // The stock ledger is opt-in; seeding it lets HasStock enforce the same materials the
        // machine supports while never blocking a colour mix (ample grams of every combo).
        foreach (var lab in labs)
        {
            foreach (var material in MaterialsByTechnology.Values.SelectMany(x => x).Distinct())
            {
                foreach (var color in Colors)
                {
                    lab.SetMaterialStock(material, color, 500_000m, reorderPointGrams: 1_000m);
                }
            }
        }
    }

    private static List<SimOrder> GenerateOrders(int count, IReadOnlyList<Machine> machines, Random random)
    {
        var orders = new List<SimOrder>(count);
        var availableTechnologies = machines.Select(m => m.Technology).Distinct().ToList();
        var maxDimensionByTechnology = machines
            .GroupBy(m => m.Technology)
            .ToDictionary(
                group => group.Key,
                group => group.Max(m => Math.Min(
                    m.BuildVolume.WidthMm,
                    Math.Min(m.BuildVolume.DepthMm, m.BuildVolume.HeightMm))));

        // Poisson-ish arrivals: exponential inter-arrival with a mean that keeps a k-order
        // stream inside roughly one 2-day window regardless of k, so the queue has real
        // contention and the four strategies produce different schedules.
        var meanGapMinutes = Math.Max(1.0, 2.0 * 24.0 * 60.0 / Math.Max(1, count));
        var arrival = EpochUtc;

        for (var i = 0; i < count; i++)
        {
            var gap = -Math.Log(1.0 - random.NextDouble()) * meanGapMinutes;
            arrival = arrival.AddMinutes(Math.Round(gap, 3));

            var technology = DrawTechnology(random, availableTechnologies);
            var materials = MaterialsByTechnology[technology];
            var material = materials[random.Next(materials.Length)];
            var color = Colors[random.Next(Colors.Length)];

            var techLimit = Math.Min(260m, maxDimensionByTechnology[technology]);
            var sizeClass = random.NextDouble();
            var (low, high) = sizeClass < 0.50 ? (40m, 100m)
                : sizeClass < 0.80 ? (100m, 180m)
                : (180m, techLimit);
            high = Math.Max(20m, Math.Min(high, techLimit));
            low = Math.Min(low, high);

            var width = Math.Min(high, low + (decimal)random.NextDouble() * (high - low));
            var depth = Math.Min(high, low + (decimal)random.NextDouble() * (high - low));
            var height = Math.Min(high, low + (decimal)random.NextDouble() * (high - low));

            var (_, jobLayer) = LayerRange(technology);
            var tolerance = decimal.Round(0.15m + (decimal)random.NextDouble() * 0.20m, 2);

            var volume = width * depth * height;
            var grams = Math.Clamp(decimal.Round(volume * 0.0007m, 1), 10m, 3_000m);
            var techFactor = technology switch
            {
                PrintTechnology.Fdm => 1.0m,
                PrintTechnology.Sla => 1.6m,
                _ => 1.2m
            };
            var estimatedMinutes = (int)Math.Clamp(
                Math.Round(volume / 8_000m * (0.20m / jobLayer) * techFactor, MidpointRounding.AwayFromZero),
                20m, 1_200m);

            // Due-date tightness: 40% tight, 40% normal, 20% loose.
            var tightness = random.NextDouble();
            var leadDays = tightness < 0.40 ? random.Next(1, 3)
                : tightness < 0.80 ? random.Next(2, 5)
                : random.Next(3, 8);

            var dueDate = DateOnly.FromDateTime(arrival.Date.AddDays(leadDays));

            var specification = JobSpecification.Create(
                BuildVolume.Create(width, depth, height),
                material,
                color,
                jobLayer,
                tolerance,
                technology,
                grams);

            var job = Job.Create(
                orderItemId: Guid.NewGuid(),
                modelId: Guid.NewGuid(),
                specification: specification,
                estimatedPrintMinutes: Math.Max(1, estimatedMinutes),
                internalDueDate: dueDate);

            orders.Add(new SimOrder
            {
                Index = i,
                Job = job,
                ArrivalUtc = arrival,
                Profile = new OrderProfile(
                    technology.ToString(),
                    material,
                    color,
                    decimal.Round(width, 1),
                    decimal.Round(depth, 1),
                    decimal.Round(height, 1),
                    jobLayer,
                    tolerance,
                    grams,
                    Math.Max(1, estimatedMinutes),
                    leadDays,
                    job.Priority.ToString())
            });
        }

        return orders;
    }

    private static PrintTechnology DrawTechnology(
        Random random,
        IReadOnlyCollection<PrintTechnology> available)
    {
        // The weighted mix applies when the network has every technology; a network with fewer
        // machines still only draws from the technologies it actually contains.
        var roll = random.NextDouble();
        if (available.Contains(PrintTechnology.Fdm))
        {
            if (roll < 0.80) return PrintTechnology.Fdm;
            if (roll < 0.90 && available.Contains(PrintTechnology.Sla)) return PrintTechnology.Sla;
            if (available.Contains(PrintTechnology.Sls)) return PrintTechnology.Sls;
            if (available.Contains(PrintTechnology.Sla)) return PrintTechnology.Sla;
            return PrintTechnology.Fdm;
        }

        var ordered = available.Order().ToList();
        return ordered[random.Next(ordered.Count)];
    }

    /// <summary>
    /// Fixed technology layer bands. These are constants (never <see cref="Random.Shared"/>)
    /// so generation stays reproducible; every order can be printed on a machine of its own
    /// technology, while tolerance and build volume still reject some cross-technology offers.
    /// </summary>
    private static (decimal MachineMinLayer, decimal JobLayer) LayerRange(PrintTechnology technology) => technology switch
    {
        PrintTechnology.Fdm => (0.14m, 0.20m),
        PrintTechnology.Sla => (0.025m, 0.05m),
        _ => (0.08m, 0.12m)
    };

    /// <summary>
    /// Canonical, GUID-free text for every order. This is the "exact generated order list hash"
    /// printed by <c>generate</c>; two runs with the same seed produce the same hash.
    /// </summary>
    public static string ComputeOrderListHash(IReadOnlyList<SimOrder> orders)
    {
        var builder = new StringBuilder();
        foreach (var order in orders)
        {
            var p = order.Profile;
            builder.Append(order.Index).Append('|')
                .Append(order.ArrivalUtc.ToString("O")).Append('|')
                .Append(order.Job.InternalDueDate.ToString("yyyy-MM-dd")).Append('|')
                .Append(p.Technology).Append('|')
                .Append(p.Material).Append('|')
                .Append(p.Color).Append('|')
                .Append(p.WidthMm.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(p.DepthMm.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(p.HeightMm.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(p.LayerHeightMm.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(p.ToleranceMm.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(p.Grams.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(p.EstimatedMinutes).Append('|')
                .Append(p.Priority)
                .AppendLine();
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
