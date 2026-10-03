using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Users.DeactivateUser;

public record DeactivateUserCommand(Guid UserId, Guid ActingUserId) : IRequest<Result>;
