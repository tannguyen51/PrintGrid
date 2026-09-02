using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty();
        RuleFor(c => c.QuoteId).NotEmpty();
        RuleFor(c => c.Street).NotEmpty().MaximumLength(200);
        RuleFor(c => c.City).NotEmpty().MaximumLength(100);
        RuleFor(c => c.District).MaximumLength(100);
        RuleFor(c => c.Ward).MaximumLength(100);
        RuleFor(c => c.PostalCode).MaximumLength(20);
    }
}
