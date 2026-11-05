namespace PrintGrid.Api.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "PrintGrid";
    public string Audience { get; init; } = "PrintGrid.Client";
    public string Key { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 60;
    public int RefreshTokenDays { get; init; } = 7;
}
