using FluentValidation;
using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.Inventory;

public record SetMaterialStockCommand(Guid LabId, string MaterialCode, string ColorCode, decimal QuantityGrams, decimal ReorderPointGrams)
    : IRequest<Result<MaterialStockDto>>;

public class SetMaterialStockCommandValidator : AbstractValidator<SetMaterialStockCommand>
{
    public SetMaterialStockCommandValidator()
    {
        RuleFor(x => x.MaterialCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ColorCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.QuantityGrams).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderPointGrams).GreaterThanOrEqualTo(0);
    }
}
