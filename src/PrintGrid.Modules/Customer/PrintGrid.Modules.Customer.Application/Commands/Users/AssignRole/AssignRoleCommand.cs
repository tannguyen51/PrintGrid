using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Users.AssignRole;

public record AssignRoleCommand(Guid UserId, string Role, Guid ActingUserId) : IRequest<Result>;
