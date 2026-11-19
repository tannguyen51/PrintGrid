using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Queries;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/scheduling/lab-scores")]
[Authorize(Policy = Policies.RequireAdmin)]
public sealed class LabScoresController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetLabScoreExplanationsQuery(), ct);
        return Ok(result);
    }
}
