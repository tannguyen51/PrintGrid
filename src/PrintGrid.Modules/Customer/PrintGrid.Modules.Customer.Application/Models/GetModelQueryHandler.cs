using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public class GetModelQueryHandler : IRequestHandler<GetModelQuery, Result<ModelDto>>
{
    private readonly IModelRepository _models;

    public GetModelQueryHandler(IModelRepository models) => _models = models;

    public async Task<Result<ModelDto>> Handle(GetModelQuery query, CancellationToken cancellationToken)
    {
        var model = await _models.GetByIdForCustomerAsync(query.CustomerId, query.ModelId, cancellationToken);
        if (model is null)
            return Result.Failure<ModelDto>(Error.NotFound("Model", query.ModelId));

        return Result.Success(ModelMappings.ToDto(model));
    }
}