namespace PrintGrid.Api.Payments;

public sealed class SePayOptions
{
    public const string SectionName = "SePay";
    public bool Enabled { get; init; }
    public string BankCode { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string WebhookApiKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public string QrBaseUrl { get; init; } = "https://vietqr.app/img";
    public bool CanCreateQr => Enabled && !string.IsNullOrWhiteSpace(BankCode) && !string.IsNullOrWhiteSpace(AccountNumber);
}
