using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Accounts;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).MaximumLength(32);
    }
}

public class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressCommandValidator() => Apply(RuleFor(x => x.Input));
    internal static void Apply(IRuleBuilderInitial<CreateAddressCommand, AddressInput> rule) => rule.ChildRules(x =>
    {
        x.RuleFor(v => v.Label).NotEmpty().MaximumLength(50);
        x.RuleFor(v => v.RecipientName).NotEmpty().MaximumLength(200);
        x.RuleFor(v => v.PhoneNumber).NotEmpty().MaximumLength(32);
        x.RuleFor(v => v.Street).NotEmpty().MaximumLength(300);
        x.RuleFor(v => v.Ward).MaximumLength(100);
        x.RuleFor(v => v.District).MaximumLength(100);
        x.RuleFor(v => v.City).NotEmpty().MaximumLength(100);
        x.RuleFor(v => v.PostalCode).MaximumLength(20);
        x.RuleFor(v => v.Country).MaximumLength(2);
    });
}

public class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x.Input).ChildRules(x =>
        {
            x.RuleFor(v => v.Label).NotEmpty().MaximumLength(50);
            x.RuleFor(v => v.RecipientName).NotEmpty().MaximumLength(200);
            x.RuleFor(v => v.PhoneNumber).NotEmpty().MaximumLength(32);
            x.RuleFor(v => v.Street).NotEmpty().MaximumLength(300);
            x.RuleFor(v => v.City).NotEmpty().MaximumLength(100);
        });
    }
}
