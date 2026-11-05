using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Customer.Application.Users;

namespace PrintGrid.Modules.Customer.Application.Commands.Users.AssignRole;

public class AssignRoleCommandHandler(ICustomerRepository customers, IUnitOfWork unitOfWork)
    : IRequestHandler<AssignRoleCommand, Result>
{
    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == request.ActingUserId)
            return Result.Failure(Error.Validation("Cannot assign role to yourself"));

        var customer = await customers.GetByIdAsync(request.UserId, cancellationToken);
        if (customer is null)
            return Result.Failure(Error.NotFound("User", request.UserId));

        customer.AssignRole(UserRoleCatalog.Canonicalize(request.Role));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
