using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
