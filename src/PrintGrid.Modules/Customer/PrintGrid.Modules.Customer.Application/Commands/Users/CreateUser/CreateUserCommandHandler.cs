using MediatR;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Customer.Application.Users;
using CustomerEntity = PrintGrid.Modules.Customer.Domain.Entities.Customer;

namespace PrintGrid.Modules.Customer.Application.Commands.Users.CreateUser;

public class CreateUserCommandHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
    : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await customers.EmailExistsAsync(request.Email, cancellationToken))
            return Result.Failure<Guid>(Error.Conflict($"Email '{request.Email}' is already registered"));

        var passwordHash = passwordHasher.Hash(request.Password);

        var customer = CustomerEntity.Register(
            request.Email,
            passwordHash,
            request.FullName,
            request.PhoneNumber);

        customer.AssignRole(UserRoleCatalog.Canonicalize(request.Role));

        await customers.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(customer.Id);
    }
}
