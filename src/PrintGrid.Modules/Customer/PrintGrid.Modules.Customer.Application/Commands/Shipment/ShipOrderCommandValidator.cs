using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

public class ShipOrderCommandValidator : AbstractValidator<ShipOrderCommand>
{
    public ShipOrderCommandValidator()
    {
        RuleFor(x => x.TrackingNumber).NotEmpty().WithMessage("Tracking number is required to ship.")
            .MaximumLength(128).WithMessage("Tracking number is too long (max 128).");
    }
}
