using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Modules.Scheduling.Application.Queries;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/network")]
public class NetworkController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Public read-only network summary for the landing page — counts only, no lab identity
    /// is exposed. Live data from the registry (no seeder, no hardcoded marketing figures).
    /// </summary>
    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetNetworkStatsQuery(), cancellationToken);
        return Ok(result.Value);
    }
}
