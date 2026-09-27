using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public class CreateModelCommandHandler : IRequestHandler<CreateModelCommand, Result<ModelDto>>
{
    private readonly IModelRepository _models;
    private readonly IUnitOfWork _unitOfWork;

    public CreateModelCommandHandler(IModelRepository models, IUnitOfWork unitOfWork)
    {
        _models = models;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ModelDto>> Handle(CreateModelCommand command, CancellationToken cancellationToken)
    {
        var model = Domain.Entities.Model.Create(
            command.CustomerId,
            command.Name,
            command.Description,
            command.FileName,
            command.FileFormat,
            command.SizeBytes,
            command.Tags);

        await _models.AddAsync(model, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ModelMappings.ToDto(model));
    }
}