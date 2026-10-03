using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Modules.Customer.Application.Commands.Users.AssignRole;
using PrintGrid.Modules.Customer.Application.Commands.Users.CreateUser;
using PrintGrid.Modules.Customer.Application.Commands.Users.DeactivateUser;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Email, request.Password, request.FullName, request.PhoneNumber, request.Role);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok(new { Id = result.Value });
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var actingUserId = Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new AssignRoleCommand(id, request.Role, actingUserId);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok();
    }

    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var actingUserId = Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new DeactivateUserCommand(id, actingUserId);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok();
    }
}

public record CreateUserRequest(string Email, string Password, string FullName, string? PhoneNumber, string Role);
public record AssignRoleRequest(string Role);
