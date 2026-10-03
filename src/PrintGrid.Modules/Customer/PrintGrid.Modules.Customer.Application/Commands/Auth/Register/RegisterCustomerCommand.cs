using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.Register;

public record RegisterCustomerCommand(
    string FullName,
    string Email,
    string Password,
    string? PhoneNumber) : IRequest<Result<AuthSessionDto>>;