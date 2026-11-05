using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public class DeleteModelCommandHandler(IModelRepository models, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteModelCommand, Result>
{
    public async Task<Result> Handle(DeleteModelCommand command, CancellationToken cancellationToken)
    {
        var model = await models.GetByIdForCustomerAsync(command.CustomerId, command.ModelId, cancellationToken);
        if (model is null)
            return Result.Failure(Error.NotFound("Model", command.ModelId));

        models.Remove(model);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
