namespace PrintGrid.Simulator.Simulation;

public sealed record Booking(DateTime StartUtc, DateTime EndUtc, bool IsDowntime);

/// <summary>
/// The in-memory machine timeline used instead of PostgreSQL. It implements only the scheduling
/// primitives the engine's <c>IMachineTimelineService</c> would provide: earliest free slot and
/// utilisation over a window, plus downtime intervals introduced by breakdowns.
/// </summary>
public sealed class MachineCalendar
{
    private readonly Dictionary<Guid, List<Booking>> _bookings = new();
    private readonly Dictionary<Guid, List<(DateTime FromUtc, DateTime ToUtc)>> _downtime = new();

    public DateTime FindEarliestFreeSlot(Guid machineId, double durationMinutes, DateTime notBeforeUtc)
    {
        var duration = Math.Max(1.0, durationMinutes);
        var cursor = notBeforeUtc;

        foreach (var booking in Bookings(machineId))
        {
            if (booking.EndUtc <= cursor) continue;
            if (cursor.AddMinutes(duration) <= booking.StartUtc) return cursor;
            if (booking.EndUtc > cursor) cursor = booking.EndUtc;
        }

        return cursor;
    }

    public decimal GetUtilization(Guid machineId, DateTime windowStartUtc, DateTime windowEndUtc)
    {
        if (windowEndUtc <= windowStartUtc) return 0m;
        var windowMinutes = (windowEndUtc - windowStartUtc).TotalMinutes;
        var busyMinutes = Bookings(machineId)
            .Where(b => !b.IsDowntime)
            .Sum(b => Math.Max(0, (Min(b.EndUtc, windowEndUtc) - Max(b.StartUtc, windowStartUtc)).TotalMinutes));

        return (decimal)Math.Clamp(busyMinutes / windowMinutes, 0.0, 1.0);
    }

    public void Book(Guid machineId, DateTime startUtc, DateTime endUtc, bool isDowntime = false)
    {
        if (endUtc <= startUtc) return;
        var list = ListFor(machineId);
        list.Add(new Booking(startUtc, endUtc, isDowntime));
        list.Sort((a, b) => a.StartUtc.CompareTo(b.StartUtc));
        if (isDowntime) DowntimeListFor(machineId).Add((startUtc, endUtc));
    }

    public void RemoveBooking(Guid machineId, DateTime startUtc, DateTime endUtc)
    {
        var list = ListFor(machineId);
        var index = list.FindIndex(b => !b.IsDowntime && b.StartUtc == startUtc && b.EndUtc == endUtc);
        if (index >= 0) list.RemoveAt(index);
    }

    /// <summary>Shortens an interrupted print to the fault instant, freeing the rest of its slot.</summary>
    public void TrimBooking(Guid machineId, DateTime startUtc, DateTime endUtc, DateTime newEndUtc)
    {
        var list = ListFor(machineId);
        var index = list.FindIndex(b => !b.IsDowntime && b.StartUtc == startUtc && b.EndUtc == endUtc);
        if (index < 0) return;

        if (newEndUtc <= startUtc) list.RemoveAt(index);
        else list[index] = list[index] with { EndUtc = newEndUtc };
    }

    /// <summary>Returns the end of the downtime interval covering <paramref name="utcTime"/>, if any.</summary>
    public DateTime? DowntimeEndAt(Guid machineId, DateTime utcTime)
    {
        if (!_downtime.TryGetValue(machineId, out var intervals)) return null;
        foreach (var interval in intervals)
        {
            if (utcTime >= interval.FromUtc && utcTime < interval.ToUtc)
                return interval.ToUtc;
        }

        return null;
    }

    public IReadOnlyList<Booking> GetAllBookings(Guid machineId) => Bookings(machineId).ToList();

    private List<Booking> ListFor(Guid machineId)
    {
        if (!_bookings.TryGetValue(machineId, out var list))
        {
            list = [];
            _bookings[machineId] = list;
        }

        return list;
    }

    private List<(DateTime, DateTime)> DowntimeListFor(Guid machineId)
    {
        if (!_downtime.TryGetValue(machineId, out var list))
        {
            list = [];
            _downtime[machineId] = list;
        }

        return list;
    }

    private IEnumerable<Booking> Bookings(Guid machineId) =>
        _bookings.TryGetValue(machineId, out var list)
            ? list.OrderBy(b => b.StartUtc)
            : Enumerable.Empty<Booking>();

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
}
