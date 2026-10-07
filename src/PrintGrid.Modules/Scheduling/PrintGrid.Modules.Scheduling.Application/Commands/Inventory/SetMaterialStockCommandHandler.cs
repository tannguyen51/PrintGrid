using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.Inventory;

public class SetMaterialStockCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    : IRequestHandler<SetMaterialStockCommand, Result<MaterialStockDto>>
{
    public async Task<Result<MaterialStockDto>> Handle(SetMaterialStockCommand command, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null) return Result.Failure<MaterialStockDto>(Error.NotFound("Lab", command.LabId));
        try
        {
            lab.SetMaterialStock(command.MaterialCode, command.ColorCode, command.QuantityGrams, command.ReorderPointGrams);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var stock = lab.MaterialStocks.Single(x => x.MaterialCode == command.MaterialCode.Trim().ToUpperInvariant()
                && x.ColorCode == command.ColorCode.Trim().ToUpperInvariant());
            return Result.Success(ToDto(stock));
        }
        catch (InvalidOperationException ex) { return Result.Failure<MaterialStockDto>(Error.Conflict(ex.Message)); }
    }

    internal static MaterialStockDto ToDto(Domain.Entities.MaterialStock x) => new(
        x.Id, x.MaterialCode, x.ColorCode, x.AvailableGrams, x.ReservedGrams,
        x.AssignableGrams, x.ReorderPointGrams, x.IsLowStock, x.UpdatedAtUtc);
}
