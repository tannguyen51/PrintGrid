using FluentValidation;
using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.Inventory;

public record SetMaterialStockCommand(Guid LabId, string MaterialCode, string ColorCode, decimal QuantityGrams, decimal ReorderPointGrams)
    : IRequest<Result<MaterialStockDto>>;

public sealed class SetMaterialStockCommandValidator : AbstractValidator<SetMaterialStockCommand>
{
    private const decimal MaximumStoredGrams = 9_999_999_999.99m;

    public SetMaterialStockCommandValidator()
    {
        RuleFor(x => x.LabId).NotEmpty();
        RuleFor(x => x.MaterialCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ColorCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.QuantityGrams).InclusiveBetween(0, MaximumStoredGrams);
        RuleFor(x => x.ReorderPointGrams).InclusiveBetween(0, MaximumStoredGrams);
    }
}
