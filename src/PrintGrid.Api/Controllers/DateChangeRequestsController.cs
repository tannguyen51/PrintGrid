using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.DateChanges;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/date-change-requests")]
[Authorize(Policy = Policies.RequireCustomer)]
public sealed class DateChangeRequestsController(ISender sender) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> Get(string token, CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        return ToResult(await sender.Send(new GetDateChangeRequestQuery(token, customerId.Value), ct));
    }

    [HttpPost("{token}/decision")]
    public async Task<IActionResult> Decide(string token, DateChangeDecisionRequest body, CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        return ToResult(await sender.Send(new DecideDateChangeCommand(token, customerId.Value, body.Approve), ct));
    }

    private IActionResult ToResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var status = result.Error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }
}

public sealed record DateChangeDecisionRequest(bool Approve);
