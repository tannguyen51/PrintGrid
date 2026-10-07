using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public record ModelQuotaDto(int UsedModels, int MaxModels, long UsedBytes, long MaxBytes);

public record GetModelQuotaQuery(Guid CustomerId, int MaxModels, long MaxBytes)
    : IRequest<Result<ModelQuotaDto>>;

public class GetModelQuotaQueryHandler(IModelRepository models)
    : IRequestHandler<GetModelQuotaQuery, Result<ModelQuotaDto>>
{
    public async Task<Result<ModelQuotaDto>> Handle(GetModelQuotaQuery request, CancellationToken cancellationToken)
    {
        var usage = await models.GetStorageUsageAsync(request.CustomerId, cancellationToken);
        return Result.Success(new ModelQuotaDto(usage.ModelCount, request.MaxModels, usage.UsedBytes, request.MaxBytes));
    }
}
