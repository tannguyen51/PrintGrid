namespace PrintGrid.Modules.Analytics.Domain.Entities;

/// <summary>Contract allocation of the frozen order-line price to a production job.</summary>
public static class LabRevenueShare
{
    public const decimal LabRate = .70m;
    public const decimal HubRate = .30m;

    public static (decimal JobValue, decimal LabAmount, decimal HubAmount) Calculate(
        decimal lineTotal, int lineQuantity, int jobQuantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lineTotal);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineQuantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(jobQuantity);
        if (jobQuantity > lineQuantity) throw new ArgumentOutOfRangeException(nameof(jobQuantity));
        var value = decimal.Round(lineTotal * jobQuantity / lineQuantity, 2);
        var lab = decimal.Round(value * LabRate, 2);
        return (value, lab, value - lab);
    }
}
