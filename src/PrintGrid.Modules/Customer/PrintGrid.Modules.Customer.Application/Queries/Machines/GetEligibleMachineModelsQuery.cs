using MediatR;
using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Queries.Machines;

public sealed record GetEligibleMachineModelsQuery(CreateQuoteCommand Configuration) : IRequest<Result<MachineModelOptions>>;

public sealed class GetEligibleMachineModelsQueryHandler(IModelRepository models, IMachineFilterService machines,
    ICommercialPolicyProvider policy) : IRequestHandler<GetEligibleMachineModelsQuery, Result<MachineModelOptions>>
{
    public async Task<Result<MachineModelOptions>> Handle(GetEligibleMachineModelsQuery query, CancellationToken ct)
    {
        var c = query.Configuration;
        var validation = new CreateQuoteCommandValidator().Validate(c);
        if (!validation.IsValid) return Result.Failure<MachineModelOptions>(Error.Validation(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))));
        var model = await models.GetByIdForCustomerAsync(c.CustomerId, c.ModelId, ct);
        if (model is null) return Result.Failure<MachineModelOptions>(Error.NotFound("Model", c.ModelId));
        if (model.BoundingWidthMm is null || model.BoundingDepthMm is null || model.BoundingHeightMm is null)
            return Result.Failure<MachineModelOptions>(Error.Validation("Model chưa được phân tích hình học xong."));
        var rates = (await policy.GetActiveAsync(ct)).Pricing;
        try
        {
            return Result.Success(await machines.GetOptionsAsync(MachineSelection.Criteria(c, model),
                rates.MinPinMachines, rates.MinPinLabs, rates.PinSurchargeCoefficient, ct));
        }
        catch (ArgumentException e) { return Result.Failure<MachineModelOptions>(Error.Validation(e.Message)); }
    }
}
