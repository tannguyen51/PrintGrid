using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.Inventory;
using PrintGrid.Modules.Scheduling.Application.Queries;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/labs/{labId:guid}/inventory")]
[Authorize(Roles = $"{Roles.LabManager},{Roles.OpsManager},{Roles.Admin}")]
public class InventoryController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid labId, CancellationToken ct)
    {
        var result = await sender.Send(new GetInventoryQuery(labId), ct);
        return result.IsFailure ? NotFound(result.Error) : Ok(result.Value);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions(Guid labId, CancellationToken ct)
    {
        var result = await sender.Send(new GetStockTransactionsQuery(labId), ct);
        return result.IsFailure ? NotFound(result.Error) : Ok(result.Value);
    }

    [HttpPut]
    public async Task<IActionResult> Set(Guid labId, [FromBody] SetMaterialStockRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SetMaterialStockCommand(labId, request.MaterialCode, request.ColorCode,
            request.QuantityGrams, request.ReorderPointGrams), ct);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpPost("adjustments")]
    public async Task<IActionResult> Adjust(Guid labId, [FromBody] AdjustMaterialStockRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new AdjustMaterialStockCommand(labId, request.MaterialCode, request.ColorCode,
            request.DeltaGrams, request.Reason), ct);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }
}

public record SetMaterialStockRequest(string MaterialCode, string ColorCode, decimal QuantityGrams, decimal ReorderPointGrams);
public record AdjustMaterialStockRequest(string MaterialCode, string ColorCode, decimal DeltaGrams, string Reason);
