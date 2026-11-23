using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.Inventory;

public record DeleteMaterialStockCommand(Guid LabId, Guid StockId) : IRequest<Result>;

public class DeleteMaterialStockCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMaterialStockCommand, Result>
{
    public async Task<Result> Handle(DeleteMaterialStockCommand command, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null) return Result.Failure(Error.NotFound("Lab", command.LabId));
        if (lab.MaterialStocks.All(item => item.Id != command.StockId))
            return Result.Failure(Error.NotFound("MaterialStock", command.StockId));

        try
        {
            lab.RemoveMaterialStock(command.StockId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure(Error.Conflict(exception.Message));
        }
    }
}
