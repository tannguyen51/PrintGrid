using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Accounts;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize(Policy = Policies.RequireCustomer)]
public class MeController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        var result = await sender.Send(new GetProfileQuery(customerId.Value), ct);
        return result.IsFailure ? NotFoundEnvelope(result.Error.Code, result.Error.Message) : Ok(result.Value);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        var result = await sender.Send(new UpdateProfileCommand(customerId.Value, request.FullName, request.PhoneNumber), ct);
        return result.IsFailure ? BadRequestEnvelope(result.Error.Code, result.Error.Message) : Ok(result.Value);
    }

    [HttpGet("addresses")]
    public async Task<IActionResult> GetAddresses(CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        var result = await sender.Send(new GetAddressesQuery(customerId.Value), ct);
        return Ok(result.Value);
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> CreateAddress([FromBody] AddressInput request, CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        var result = await sender.Send(new CreateAddressCommand(customerId.Value, request), ct);
        return result.IsFailure ? BadRequestEnvelope(result.Error.Code, result.Error.Message)
            : CreatedAtAction(nameof(GetAddresses), result.Value);
    }

    [HttpPut("addresses/{addressId:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid addressId, [FromBody] AddressInput request, CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        var result = await sender.Send(new UpdateAddressCommand(customerId.Value, addressId, request), ct);
        return result.IsFailure ? NotFoundEnvelope(result.Error.Code, result.Error.Message) : Ok(result.Value);
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        var result = await sender.Send(new DeleteAddressCommand(customerId.Value, addressId), ct);
        if (result.IsSuccess) return NoContent();
        return result.Error.Code == "conflict"
            ? Conflict(new { error = new { code = result.Error.Code, message = result.Error.Message } })
            : NotFoundEnvelope(result.Error.Code, result.Error.Message);
    }

    private ObjectResult NotFoundEnvelope(string code, string message) =>
        NotFound(new { error = new { code, message } });
    private ObjectResult BadRequestEnvelope(string code, string message) =>
        BadRequest(new { error = new { code, message } });
}

public record UpdateProfileRequest(string FullName, string? PhoneNumber);
