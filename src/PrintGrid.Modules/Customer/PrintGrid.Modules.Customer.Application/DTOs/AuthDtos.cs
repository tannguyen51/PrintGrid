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

public record AuthSessionDto(
    string AccessToken,
    string RefreshToken,
    AuthUserDto User);

public record AuthTokenPairDto(
    string AccessToken,
    string RefreshToken);