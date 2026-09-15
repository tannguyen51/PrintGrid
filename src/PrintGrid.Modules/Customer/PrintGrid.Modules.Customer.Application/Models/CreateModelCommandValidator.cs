using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Models;

public class CreateModelCommandValidator : AbstractValidator<CreateModelCommand>
{
    public CreateModelCommandValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.FileName).NotEmpty().MaximumLength(255);
        RuleFor(c => c.FileFormat).NotEmpty().MaximumLength(10);
        RuleFor(c => c.SizeBytes).GreaterThanOrEqualTo(0);
        RuleForEach(c => c.Tags).MaximumLength(50);
    }
}