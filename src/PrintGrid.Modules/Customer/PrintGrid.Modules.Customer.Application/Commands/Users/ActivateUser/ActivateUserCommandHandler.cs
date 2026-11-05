using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Users.ActivateUser;

public sealed class ActivateUserCommandHandler(ICustomerRepository customers, IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateUserCommand, Result>
{
    public async Task<Result> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(request.UserId, cancellationToken);
        if (customer is null) return Result.Failure(Error.NotFound("User", request.UserId));

        customer.Activate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
