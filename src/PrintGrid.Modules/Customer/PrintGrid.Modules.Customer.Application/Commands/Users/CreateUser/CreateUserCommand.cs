using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Users.CreateUser;

public record CreateUserCommand(string Email, string Password, string FullName, string? PhoneNumber, string Role) : IRequest<Result<Guid>>;
