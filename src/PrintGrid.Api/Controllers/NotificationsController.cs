using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using PrintGrid.Api.Authorization;
using PrintGrid.Api.Hubs;
using PrintGrid.Infrastructure.Shared.Persistence;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private const int PageSize = 30;
    private readonly PrintGridDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationsController(
        PrintGridDbContext dbContext,
        IHubContext<NotificationHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        var userId = User.GetCustomerId();
        if (userId is null) return Forbid();

        var notifications = await _dbContext.UserNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(PageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.Link,
                n.CreatedAtUtc,
                n.ReadAtUtc))
            .ToListAsync(cancellationToken);

        var unreadCount = await _dbContext.UserNotifications
            .CountAsync(n => n.UserId == userId.Value && n.ReadAtUtc == null, cancellationToken);

        return Ok(new { items = notifications, unreadCount });
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetCustomerId();
        if (userId is null) return Forbid();

        var notification = await _dbContext.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value, cancellationToken);
        if (notification is null) return NotFound();

        notification.MarkAsRead();
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _hubContext.Clients.Group(NotificationHub.UserGroup(userId.Value))
            .SendAsync("NotificationsUpdated", cancellationToken);

        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = User.GetCustomerId();
        if (userId is null) return Forbid();

        var unreadNotifications = await _dbContext.UserNotifications
            .Where(n => n.UserId == userId.Value && n.ReadAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _hubContext.Clients.Group(NotificationHub.UserGroup(userId.Value))
            .SendAsync("NotificationsUpdated", cancellationToken);

        return NoContent();
    }

    public sealed record NotificationDto(
        Guid Id,
        string Title,
        string Message,
        string Type,
        string? Link,
        DateTime CreatedAtUtc,
        DateTime? ReadAtUtc);
}
