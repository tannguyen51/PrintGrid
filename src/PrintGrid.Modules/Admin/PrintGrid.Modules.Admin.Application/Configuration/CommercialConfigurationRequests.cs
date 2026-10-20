using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;

namespace PrintGrid.Modules.Admin.Application.Configuration;

public sealed record GetCommercialConfigurationsQuery : IRequest<IReadOnlyList<CommercialConfigurationDto>>;
public sealed record CreateCommercialConfigurationCommand(
    string Name, CommercialParametersDto Parameters, string ChangeSummary, Guid ActorId)
    : IRequest<CommercialConfigurationDto>;
public sealed record ActivateCommercialConfigurationCommand(Guid Id) : IRequest<MutationResult<CommercialConfigurationDto>>;

public sealed class CommercialParametersValidator : AbstractValidator<CommercialParametersDto>
{
    private static readonly string[] WeightKeys = ["time", "load", "quality", "cost", "logistics"];

    public CommercialParametersValidator()
    {
        RuleFor(x => x.MaterialRates).Cascade(CascadeMode.Stop).NotEmpty().Must(x => x.All(p => !string.IsNullOrWhiteSpace(p.Key) && p.Value > 0))
            .WithMessage("Mỗi vật liệu phải có đơn giá lớn hơn 0.");
        RuleFor(x => x.MaterialDensities).Cascade(CascadeMode.Stop).NotEmpty().Must(x => x.All(p => !string.IsNullOrWhiteSpace(p.Key) && p.Value > 0))
            .WithMessage("Mỗi vật liệu phải có khối lượng riêng lớn hơn 0.");
        RuleFor(x => x.PricePerPrintHour).GreaterThan(0);
        RuleFor(x => x.HubHandlingFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PlatformMarginPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.PrioritySurchargePercent).InclusiveBetween(0, 500);
        RuleFor(x => x.AutoLaneThreshold).GreaterThanOrEqualTo(0);
        RuleFor(x => x.QuoteValidityHours).InclusiveBetween(1, 720);
        RuleFor(x => x.MaxBatchSize).InclusiveBetween(1, 1000);
        RuleFor(x => x.SchedulingTimeBudgetSeconds).InclusiveBetween(1, 300);
        RuleFor(x => x.SlaRiskThresholdHours).InclusiveBetween(1, 720);
        RuleFor(x => x.AssignmentWeights).Cascade(CascadeMode.Stop).NotEmpty().Must(weights =>
                WeightKeys.All(weights.ContainsKey) && weights.Values.All(v => v >= 0 && v <= 1) &&
                Math.Abs(weights.Values.Sum() - 1m) <= 0.001m)
            .WithMessage("Trọng số phải đủ 5 tiêu chí, nằm trong [0,1] và có tổng bằng 1.");
    }
}

public sealed class CreateCommercialConfigurationValidator : AbstractValidator<CreateCommercialConfigurationCommand>
{
    public CreateCommercialConfigurationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ChangeSummary).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Parameters).NotNull().SetValidator(new CommercialParametersValidator());
    }
}

public sealed class CommercialConfigurationHandlers(IAdminConfigurationRepository repository) :
    IRequestHandler<GetCommercialConfigurationsQuery, IReadOnlyList<CommercialConfigurationDto>>,
    IRequestHandler<CreateCommercialConfigurationCommand, CommercialConfigurationDto>,
    IRequestHandler<ActivateCommercialConfigurationCommand, MutationResult<CommercialConfigurationDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CommercialConfigurationDto>> Handle(GetCommercialConfigurationsQuery request, CancellationToken cancellationToken) =>
        (await repository.GetConfigurationsAsync(cancellationToken)).Select(Map).ToList();

    public async Task<CommercialConfigurationDto> Handle(CreateCommercialConfigurationCommand request, CancellationToken cancellationToken)
    {
        var materials = await repository.GetCatalogAsync(SharedCatalogTypes.Material, false, cancellationToken);
        var expectedCodes = materials.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rateCodes = request.Parameters.MaterialRates.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var densityCodes = request.Parameters.MaterialDensities.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!expectedCodes.SetEquals(rateCodes) || !expectedCodes.SetEquals(densityCodes))
            throw new ValidationException([new ValidationFailure("Parameters.MaterialRates", "Bảng giá và khối lượng riêng phải bao phủ đúng các vật liệu đang hoạt động trong danh mục dùng chung.")]);
        if (materials.Any(x => x.UnitRate is not null && request.Parameters.MaterialRates[x.Code] != x.UnitRate.Value))
            throw new ValidationException([new ValidationFailure("Parameters.MaterialRates", "Đơn giá vật liệu phải khớp với revision hiện hành trong danh mục dùng chung.")]);

        var number = await repository.GetNextConfigurationVersionAsync(cancellationToken);
        var entity = CommercialConfigurationVersion.Create(number, request.Name,
            JsonSerializer.Serialize(request.Parameters, JsonOptions), request.ChangeSummary, request.ActorId);
        await repository.AddConfigurationAsync(entity, cancellationToken);
        return Map(entity);
    }

    public async Task<MutationResult<CommercialConfigurationDto>> Handle(ActivateCommercialConfigurationCommand request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetConfigurationAsync(request.Id, cancellationToken);
        if (entity is null) return MutationResult<CommercialConfigurationDto>.Failure("configuration_not_found", "Không tìm thấy phiên bản cấu hình.");
        if (entity.Status != ConfigurationStatuses.Draft)
            return MutationResult<CommercialConfigurationDto>.Failure("configuration_not_draft", "Chỉ phiên bản nháp mới được kích hoạt.");
        var previous = await repository.GetActiveConfigurationAsync(cancellationToken);
        await repository.ActivateConfigurationAsync(entity, previous, cancellationToken);
        return MutationResult<CommercialConfigurationDto>.Success(Map(entity));
    }

    private static CommercialConfigurationDto Map(CommercialConfigurationVersion x) => new(
        x.Id, x.VersionNumber, x.Name, x.Status,
        JsonSerializer.Deserialize<CommercialParametersDto>(x.ParametersJson, JsonOptions)!,
        x.ChangeSummary, x.CreatedBy, x.CreatedAtUtc, x.ActivatedAtUtc);
}
