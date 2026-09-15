using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.Refresh;

public record RefreshSessionCommand(
    string RefreshToken) : IRequest<Result<AuthSessionDto>>;