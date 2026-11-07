using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// Model pinning eligibility check (BR-04).
/// Returns machine models that qualify for customer model selection.
/// </summary>
[ApiController]
[Route("api/v1/models")]
[Authorize(Policy = Policies.RequireCustomer)]
public sealed class MachinesModelsController(ISender sender) : ControllerBase
{
    [HttpGet("eligible-for-pinning")]
    public async Task<IActionResult> GetEligibleForPinning([FromQuery] Guid? materialCode, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEligibleMachineModelsQuery(materialCode), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }
}
