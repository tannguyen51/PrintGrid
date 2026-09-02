namespace PrintGrid.Modules.Customer.Domain.Enums;

public enum OrderStatus
{
    PaymentPending,
    Confirmed,
    InProduction,
    QualityCheck,
    Shipping,
    Delivered,
    Cancelled
}
