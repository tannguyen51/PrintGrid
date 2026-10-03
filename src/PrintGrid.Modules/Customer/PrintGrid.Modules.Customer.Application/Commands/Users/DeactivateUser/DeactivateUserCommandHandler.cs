using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Users.DeactivateUser;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUserCommandHandler(ICustomerRepository customers, IUnitOfWork unitOfWork)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == request.ActingUserId)
            return Result.Failure(Error.Validation("Cannot deactivate yourself"));

        var customer = await _customers.GetByIdAsync(request.UserId, cancellationToken);
        if (customer is null)
            return Result.Failure(Error.NotFound("User", request.UserId));

        customer.Deactivate();
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
