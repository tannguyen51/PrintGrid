namespace PrintGrid.Infrastructure.Shared.Persistence.Notifications;

public sealed class UserNotification
{
    private UserNotification() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public static UserNotification Create(
        Guid userId,
        string title,
        string message,
        string type,
        string? link = null)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message is required.", nameof(message));
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type is required.", nameof(type));

        return new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            Type = type.Trim(),
            Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void MarkAsRead()
    {
        ReadAtUtc ??= DateTime.UtcNow;
    }
}
