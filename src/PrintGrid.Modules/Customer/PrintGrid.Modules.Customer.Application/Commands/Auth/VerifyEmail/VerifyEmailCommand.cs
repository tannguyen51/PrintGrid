using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.VerifyEmail;

public record VerifyEmailCommand(string Email, string Token) : IRequest<Result>;
