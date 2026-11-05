using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public class UpdateModelCommandHandler(IModelRepository models, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateModelCommand, Result<ModelDto>>
{
    public async Task<Result<ModelDto>> Handle(UpdateModelCommand command, CancellationToken cancellationToken)
    {
        var model = await models.GetByIdForCustomerAsync(command.CustomerId, command.ModelId, cancellationToken);
        if (model is null)
            return Result.Failure<ModelDto>(Error.NotFound("Model", command.ModelId));

        model.UpdateDetails(
            command.Name,
            command.Description,
            command.FileName,
            command.FileFormat,
            command.SizeBytes,
            command.Tags);

        models.Update(model);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ModelMappings.ToDto(model));
    }
}
