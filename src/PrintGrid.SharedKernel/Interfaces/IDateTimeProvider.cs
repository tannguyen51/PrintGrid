namespace PrintGrid.SharedKernel.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
