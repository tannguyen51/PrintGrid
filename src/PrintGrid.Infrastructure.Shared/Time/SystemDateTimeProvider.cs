using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Infrastructure.Shared.Time;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
