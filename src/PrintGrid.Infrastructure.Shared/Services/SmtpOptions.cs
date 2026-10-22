namespace PrintGrid.Infrastructure.Shared.Services;

public sealed class SmtpOptions
{
    public const string SectionName = "Email:Smtp";

    public bool Enabled { get; init; }
    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "PrintGrid";
}
