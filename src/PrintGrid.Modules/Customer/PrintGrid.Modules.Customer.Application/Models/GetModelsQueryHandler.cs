using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public class GetModelsQueryHandler : IRequestHandler<GetModelsQuery, Result<IReadOnlyList<ModelDto>>>
{
    private readonly IModelRepository _models;

    public GetModelsQueryHandler(IModelRepository models) => _models = models;

    public async Task<Result<IReadOnlyList<ModelDto>>> Handle(
        GetModelsQuery query,
        CancellationToken cancellationToken)
    {
        var modelEntities = await _models.GetForCustomerAsync(query.CustomerId, query.Search, cancellationToken);
        var dtos = modelEntities.Select(ModelMappings.ToDto).ToList();
        return Result.Success<IReadOnlyList<ModelDto>>(dtos);
    }
}