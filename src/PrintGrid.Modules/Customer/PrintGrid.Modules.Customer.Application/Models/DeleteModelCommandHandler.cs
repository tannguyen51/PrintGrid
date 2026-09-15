using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public class DeleteModelCommandHandler : IRequestHandler<DeleteModelCommand, Result>
{
    private readonly IModelRepository _models;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteModelCommandHandler(IModelRepository models, IUnitOfWork unitOfWork)
    {
        _models = models;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteModelCommand command, CancellationToken cancellationToken)
    {
        var model = await _models.GetByIdForCustomerAsync(command.CustomerId, command.ModelId, cancellationToken);
        if (model is null)
            return Result.Failure(Error.NotFound("Model", command.ModelId));

        _models.Remove(model);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}