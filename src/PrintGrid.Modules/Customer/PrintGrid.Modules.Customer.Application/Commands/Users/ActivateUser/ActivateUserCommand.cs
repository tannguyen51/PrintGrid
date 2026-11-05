using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Users.ActivateUser;

public record ActivateUserCommand(Guid UserId) : IRequest<Result>;
