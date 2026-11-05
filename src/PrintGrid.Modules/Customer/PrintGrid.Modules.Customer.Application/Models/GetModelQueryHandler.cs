using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public class GetModelQueryHandler(IModelRepository models) : IRequestHandler<GetModelQuery, Result<ModelDto>>
{
    public async Task<Result<ModelDto>> Handle(GetModelQuery query, CancellationToken cancellationToken)
    {
        var model = await models.GetByIdForCustomerAsync(query.CustomerId, query.ModelId, cancellationToken);
        return model is null ? Result.Failure<ModelDto>(Error.NotFound("Model", query.ModelId)) : Result.Success(ModelMappings.ToDto(model));
    }
}
