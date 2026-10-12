using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Modules.Customer.Application.Commands.Users.AssignRole;
using PrintGrid.Modules.Customer.Application.Commands.Users.CreateUser;
using PrintGrid.Modules.Customer.Application.Commands.Users.DeactivateUser;
using PrintGrid.Modules.Customer.Application.Queries.Users;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Admin.Application.AuditLogs;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuditLogWriter _audit;

    public UsersController(IMediator mediator, IAuditLogWriter audit)
    {
        _mediator = mediator;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUsersQuery(), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Email, request.Password, request.FullName, request.PhoneNumber, request.Role);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        await WriteAudit("user.create", result.Value.ToString(), null,
            new { request.Email, request.FullName, request.PhoneNumber, request.Role }, cancellationToken);
        return Ok(new { Id = result.Value });
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var actingUserId = Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var before = (await _mediator.Send(new GetUsersQuery(), cancellationToken)).Value
            .FirstOrDefault(x => x.Id == id);
        var command = new AssignRoleCommand(id, request.Role, actingUserId);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        await WriteAudit("user.role_changed", id.ToString(), new { before?.Roles },
            new { Roles = new[] { request.Role } }, cancellationToken);
        return Ok();
    }

    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var actingUserId = Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var before = (await _mediator.Send(new GetUsersQuery(), cancellationToken)).Value
            .FirstOrDefault(x => x.Id == id);
        var command = new DeactivateUserCommand(id, actingUserId);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error);
        await WriteAudit("user.deactivated", id.ToString(), new { before?.IsActive },
            new { IsActive = false }, cancellationToken);
        return Ok();
    }

    private Task WriteAudit(string action, string entityId, object? oldValues, object? newValues, CancellationToken cancellationToken)
    {
        var actorId = Guid.TryParse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (Guid?)null;
        return _audit.WriteAsync(new AuditLogWriteRequest(
            actorId,
            User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "unknown",
            User.FindFirstValue(ClaimTypes.Role) ?? "Admin",
            AuditActionClasses.Configuration,
            action,
            "User",
            entityId,
            oldValues,
            newValues,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            TraceId: HttpContext.TraceIdentifier,
            RequirementCodes: ["FR-ADMIN-001", "FR-ADMIN-004", "UC-022", "UC-025", "BR-ACCESS-005", "BR-CONFIG-002", "BR-CONFIG-004"]),
            cancellationToken);
    }
}

public record CreateUserRequest(string Email, string Password, string FullName, string? PhoneNumber, string Role);
public record AssignRoleRequest(string Role);
