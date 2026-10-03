using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.ResendVerification;

public record ResendVerificationCommand(string Email) : IRequest<Result<ResendVerificationResponse>>;

public record ResendVerificationResponse(int WaitTimeSeconds);
