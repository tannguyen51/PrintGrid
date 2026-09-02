using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Domain.Entities;

public class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ModelId { get; private set; }
    public int Quantity { get; private set; }
    public PrintConfiguration Configuration { get; private set; } = null!;
    public Money UnitPrice { get; private set; } = Money.Zero();

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderItem() { }

    internal static OrderItem FromQuoteItem(Guid orderId, QuoteItem quoteItem) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        ModelId = quoteItem.ModelId,
        Quantity = quoteItem.Quantity,
        Configuration = quoteItem.Configuration,
        UnitPrice = quoteItem.UnitPrice
    };
}
