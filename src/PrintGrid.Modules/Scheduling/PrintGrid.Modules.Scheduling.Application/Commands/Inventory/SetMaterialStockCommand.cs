using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.Inventory;

public record SetMaterialStockCommand(Guid LabId, string MaterialCode, string ColorCode, decimal QuantityGrams, decimal ReorderPointGrams)
    : IRequest<Result<MaterialStockDto>>;
