namespace PrintGrid.Modules.Customer.Application.DTOs;

public record ModelDto(
    Guid Id,
    string Name,
    string? Description,
    string FileName,
    string FileFormat,
    long SizeBytes,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string GeometryStatus,
    decimal? BoundingWidthMm,
    decimal? BoundingDepthMm,
    decimal? BoundingHeightMm,
    decimal? VolumeCm3,
    int? EstimatedPrintMinutes,
    string? GeometryMessage,
    string? StorageKey);

public record AuthUserDto(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);

public record QuoteDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal TotalPrice,
    string Currency,
    DateOnly PromisedDeliveryDate,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string? FailureReason,
    IReadOnlyList<QuoteItemDto> Items);

public record QuoteItemDto(
    Guid Id,
    Guid ModelId,
    int Quantity,
    string MaterialCode,
    string ColorCode,
    decimal LayerHeightMm,
    int InfillPercent,
    decimal UnitPrice,
    int EstimatedPrintMinutes,
    decimal EstimatedMaterialGrams);

public record AuthSessionDto(
    string AccessToken,
    string RefreshToken,
    AuthUserDto User);

public record AuthTokenPairDto(
    string AccessToken,
    string RefreshToken);