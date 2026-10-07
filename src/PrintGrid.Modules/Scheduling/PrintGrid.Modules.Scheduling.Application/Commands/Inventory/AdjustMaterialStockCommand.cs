using FluentValidation;
using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.Inventory;

public record AdjustMaterialStockCommand(Guid LabId, string MaterialCode, string ColorCode, decimal DeltaGrams, string Reason)
    : IRequest<Result<MaterialStockDto>>;

public class AdjustMaterialStockCommandValidator : AbstractValidator<AdjustMaterialStockCommand>
{
    public AdjustMaterialStockCommandValidator()
    {
        RuleFor(x => x.MaterialCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ColorCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DeltaGrams).NotEqual(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(250);
    }
}

public class AdjustMaterialStockCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    : IRequestHandler<AdjustMaterialStockCommand, Result<MaterialStockDto>>
{
    public async Task<Result<MaterialStockDto>> Handle(AdjustMaterialStockCommand command, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null) return Result.Failure<MaterialStockDto>(Error.NotFound("Lab", command.LabId));
        try
        {
            lab.AdjustMaterialStock(command.MaterialCode, command.ColorCode, command.DeltaGrams, command.Reason);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var stock = lab.MaterialStocks.Single(x => x.MaterialCode == command.MaterialCode.Trim().ToUpperInvariant()
                && x.ColorCode == command.ColorCode.Trim().ToUpperInvariant());
            return Result.Success(SetMaterialStockCommandHandler.ToDto(stock));
        }
        catch (InvalidOperationException ex) { return Result.Failure<MaterialStockDto>(Error.Conflict(ex.Message)); }
    }
}
