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
            var stockIds = lab.MaterialStocks.Select(x => x.Id).ToHashSet();
            var transactionIds = lab.StockTransactions.Select(x => x.Id).ToHashSet();

            lab.SetMaterialStock(command.MaterialCode, command.ColorCode, command.QuantityGrams, command.ReorderPointGrams);

            // Children added through a loaded aggregate with client-generated IDs can be inferred as
            // Modified by EF. Explicitly register new ledger rows so SaveChanges issues INSERTs.
            foreach (var newStock in lab.MaterialStocks.Where(x => !stockIds.Contains(x.Id)))
                labs.AddMaterialStock(newStock);
            foreach (var transaction in lab.StockTransactions.Where(x => !transactionIds.Contains(x.Id)))
                labs.AddStockTransaction(transaction);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            var stock = lab.MaterialStocks.Single(x => x.MaterialCode.Equals(command.MaterialCode.Trim(), StringComparison.InvariantCultureIgnoreCase)
                && x.ColorCode.Equals(command.ColorCode.Trim(), StringComparison.InvariantCultureIgnoreCase));
            return Result.Success(ToDto(stock));
        }
        catch (InvalidOperationException ex) { return Result.Failure<MaterialStockDto>(Error.Conflict(ex.Message)); }
    }

    internal static MaterialStockDto ToDto(Domain.Entities.MaterialStock x) => new(
        x.Id, x.MaterialCode, x.ColorCode, x.AvailableGrams, x.ReservedGrams,
        x.AssignableGrams, x.ReorderPointGrams, x.IsLowStock, x.UpdatedAtUtc);
}
