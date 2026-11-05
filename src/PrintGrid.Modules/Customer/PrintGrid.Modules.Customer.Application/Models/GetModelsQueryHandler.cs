using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public class GetModelsQueryHandler(IModelRepository models)
    : IRequestHandler<GetModelsQuery, Result<IReadOnlyList<ModelDto>>>
{
    public async Task<Result<IReadOnlyList<ModelDto>>> Handle(
        GetModelsQuery query,
        CancellationToken cancellationToken)
    {
        var modelEntities = await models.GetForCustomerAsync(query.CustomerId, query.Search, cancellationToken);
        var dtos = modelEntities.Select(ModelMappings.ToDto).ToList();
        return Result.Success<IReadOnlyList<ModelDto>>(dtos);
    }
}
