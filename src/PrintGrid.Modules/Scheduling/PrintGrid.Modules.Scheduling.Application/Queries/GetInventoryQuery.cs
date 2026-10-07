using MediatR;
using PrintGrid.Modules.Scheduling.Application.Commands.Inventory;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public record GetInventoryQuery(Guid LabId) : IRequest<Result<IReadOnlyList<MaterialStockDto>>>;
public record GetStockTransactionsQuery(Guid LabId) : IRequest<Result<IReadOnlyList<StockTransactionDto>>>;

public class GetInventoryQueryHandler(ILabRepository labs) : IRequestHandler<GetInventoryQuery, Result<IReadOnlyList<MaterialStockDto>>>
{
    public async Task<Result<IReadOnlyList<MaterialStockDto>>> Handle(GetInventoryQuery query, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(query.LabId, cancellationToken);
        if (lab is null) return Result.Failure<IReadOnlyList<MaterialStockDto>>(Error.NotFound("Lab", query.LabId));
        return Result.Success<IReadOnlyList<MaterialStockDto>>(lab.MaterialStocks.OrderBy(x => x.MaterialCode)
            .ThenBy(x => x.ColorCode).Select(SetMaterialStockCommandHandler.ToDto).ToList());
    }
}

public class GetStockTransactionsQueryHandler(ILabRepository labs) : IRequestHandler<GetStockTransactionsQuery, Result<IReadOnlyList<StockTransactionDto>>>
{
    public async Task<Result<IReadOnlyList<StockTransactionDto>>> Handle(GetStockTransactionsQuery query, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(query.LabId, cancellationToken);
        if (lab is null) return Result.Failure<IReadOnlyList<StockTransactionDto>>(Error.NotFound("Lab", query.LabId));
        return Result.Success<IReadOnlyList<StockTransactionDto>>(lab.StockTransactions.OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new StockTransactionDto(x.TransactionCode, x.MaterialStockId, x.DeltaGrams, x.RunningTotalGrams,
                x.Reason, x.JobId, x.CreatedAtUtc)).ToList());
    }
}
