using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Queries.Users;

/// <summary>
/// Every account with its roles, for the admin user-management screen (FR-ADMIN-001).
/// Read-only: creating, re-roling and deactivating stay in the existing commands.
/// </summary>
public record GetUsersQuery : IRequest<Result<IReadOnlyList<AdminUserDto>>>;