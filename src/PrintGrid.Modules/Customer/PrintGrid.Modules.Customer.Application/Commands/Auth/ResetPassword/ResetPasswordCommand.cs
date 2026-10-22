using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Result>;
