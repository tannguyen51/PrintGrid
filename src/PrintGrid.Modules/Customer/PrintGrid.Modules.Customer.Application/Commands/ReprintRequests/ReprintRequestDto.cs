namespace PrintGrid.Modules.Customer.Application.Commands.ReprintRequests;

public sealed record ReprintRequestDto(
    Guid RequestId,
    Guid OrderId,
    string Reason,
    string Description,
    IReadOnlyList<string> Photos,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? ResolutionNote);
