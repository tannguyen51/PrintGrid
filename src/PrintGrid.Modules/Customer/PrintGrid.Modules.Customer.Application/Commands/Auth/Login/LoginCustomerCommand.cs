using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.Login;

public record LoginCustomerCommand(
    string Email,
    string Password) : IRequest<Result<AuthSessionDto>>;