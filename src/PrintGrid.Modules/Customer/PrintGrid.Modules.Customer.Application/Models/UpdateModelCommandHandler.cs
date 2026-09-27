using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public class UpdateModelCommandHandler : IRequestHandler<UpdateModelCommand, Result<ModelDto>>
{
    private readonly IModelRepository _models;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateModelCommandHandler(IModelRepository models, IUnitOfWork unitOfWork)
    {
        _models = models;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ModelDto>> Handle(UpdateModelCommand command, CancellationToken cancellationToken)
    {
        var model = await _models.GetByIdForCustomerAsync(command.CustomerId, command.ModelId, cancellationToken);
        if (model is null)
            return Result.Failure<ModelDto>(Error.NotFound("Model", command.ModelId));

        model.UpdateDetails(
            command.Name,
            command.Description,
            command.FileName,
            command.FileFormat,
            command.SizeBytes,
            command.Tags);

        _models.Update(model);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ModelMappings.ToDto(model));
    }
}