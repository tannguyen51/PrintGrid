namespace PrintGrid.Modules.Customer.Application.DTOs;

/// <summary>One account as the admin screen sees it — never carries the password hash.</summary>
public record AdminUserDto(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    bool IsActive,
    bool IsEmailVerified,
    DateTime CreatedAt,
    DateTime? LastLoginAt);