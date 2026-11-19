namespace PrintGrid.Modules.Customer.Domain.Enums;

public enum OrderStatus
{
    AwaitingApproval,
    PaymentPending,
    Confirmed,
    InProduction,
    QualityCheck,
    Shipping,
    Delivered,
    Cancelled
}
