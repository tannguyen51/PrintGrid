using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Commands.Auth.CurrentUser;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// Session introspection for any signed-in role. AuthController is <c>[AllowAnonymous]</c> as a
/// whole (it is the login surface), and AllowAnonymous cannot be overridden per action, so this
/// authenticated endpoint lives in its own controller.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Authorize]
public sealed class AuthSessionController(ISender sender) : ControllerBase
{
    /// <summary>
    /// The caller's current account row, including roles as they are in the database right now.
    /// The frontend uses this to resync the session after a role change instead of making staff
    /// sign out and back in to pick up their new console.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetCustomerId();
        if (userId is null) return Unauthorized();

        var result = await sender.Send(new GetCurrentUserQuery(userId.Value), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized();
    }
}
