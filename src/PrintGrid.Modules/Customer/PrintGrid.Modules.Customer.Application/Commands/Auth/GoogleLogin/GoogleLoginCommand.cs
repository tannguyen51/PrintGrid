using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.GoogleLogin;

public sealed record GoogleLoginCommand(string Credential) : IRequest<Result<AuthSessionDto>>;
