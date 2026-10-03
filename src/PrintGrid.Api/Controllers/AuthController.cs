using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Modules.Customer.Application.Commands.Auth.Login;
using PrintGrid.Modules.Customer.Application.Commands.Auth.Refresh;
using PrintGrid.Modules.Customer.Application.Commands.Auth.Register;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterCustomerCommand(request.FullName, request.Email, request.Password, request.PhoneNumber),
            cancellationToken);

        return result.IsFailure
            ? Conflict(new { error = new { code = result.Error.Code, message = result.Error.Message } })
            : Created("/api/v1/auth/login", result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCustomerCommand(request.Email, request.Password),
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "unauthorized"
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RefreshSessionCommand(request.RefreshToken),
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "unauthorized"
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        return Ok(result.Value);
    }
}

public record RegisterRequest(string FullName, string Email, string Password, string? PhoneNumber);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);