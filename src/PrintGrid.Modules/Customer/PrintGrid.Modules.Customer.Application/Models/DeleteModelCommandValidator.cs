using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Models;

public class DeleteModelCommandValidator : AbstractValidator<DeleteModelCommand>
{
    public DeleteModelCommandValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty();
        RuleFor(c => c.ModelId).NotEmpty();
    }
}