using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Simulator.Simulation;

/// <summary>
/// The VIRTUAL CLOCK seam: simulation code only reads time through
/// <see cref="IDateTimeProvider"/>, never <c>DateTime.UtcNow</c> (AC TEST-003 / task RUN-04).
/// </summary>
public sealed class VirtualClock : IDateTimeProvider
{
    private DateTime _utcNow;

    public VirtualClock(DateTime startUtc)
    {
        _utcNow = Normalize(startUtc);
    }

    public DateTime UtcNow => _utcNow;

    public DateOnly Today => DateOnly.FromDateTime(_utcNow);

    /// <summary>Moves the clock forward; it never runs backwards (a discrete-event invariant).</summary>
    public void AdvanceTo(DateTime utcTime)
    {
        var target = Normalize(utcTime);
        if (target > _utcNow) _utcNow = target;
    }

    private static DateTime Normalize(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
