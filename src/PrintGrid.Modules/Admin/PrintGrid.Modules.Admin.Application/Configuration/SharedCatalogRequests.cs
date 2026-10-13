using System.Text.Json;
using FluentValidation;
using MediatR;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;

namespace PrintGrid.Modules.Admin.Application.Configuration;

public sealed record GetSharedCatalogQuery(string? CatalogType, bool IncludeDeprecated) : IRequest<IReadOnlyList<SharedCatalogItemDto>>;
public sealed record SaveSharedCatalogItemCommand(
    string CatalogType, string Code, string Name, string? Description, decimal? UnitRate,
    bool IsPremium, JsonElement Properties, bool IsDeprecated, Guid ActorId)
    : IRequest<MutationResult<SharedCatalogItemDto>>;
public sealed record DeleteSharedCatalogItemCommand(string CatalogType, string Code)
    : IRequest<MutationResult<bool>>;

public sealed class SaveSharedCatalogItemValidator : AbstractValidator<SaveSharedCatalogItemCommand>
{
    public SaveSharedCatalogItemValidator()
    {
        RuleFor(x => x.CatalogType).Must(x => SharedCatalogTypes.All.Contains(x)).WithMessage("Loại danh mục không hợp lệ.");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.UnitRate).GreaterThanOrEqualTo(0).When(x => x.UnitRate.HasValue);
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class SharedCatalogHandlers(IAdminConfigurationRepository repository) :
    IRequestHandler<GetSharedCatalogQuery, IReadOnlyList<SharedCatalogItemDto>>,
    IRequestHandler<SaveSharedCatalogItemCommand, MutationResult<SharedCatalogItemDto>>,
    IRequestHandler<DeleteSharedCatalogItemCommand, MutationResult<bool>>
{
    public async Task<IReadOnlyList<SharedCatalogItemDto>> Handle(GetSharedCatalogQuery request, CancellationToken cancellationToken)
    {
        var type = string.IsNullOrWhiteSpace(request.CatalogType) ? null : CanonicalType(request.CatalogType);
        return (await repository.GetCatalogAsync(type, request.IncludeDeprecated, cancellationToken)).Select(Map).ToList();
    }

    public async Task<MutationResult<SharedCatalogItemDto>> Handle(SaveSharedCatalogItemCommand request, CancellationToken cancellationToken)
    {
        var type = CanonicalType(request.CatalogType);
        var code = request.Code.Trim().ToUpperInvariant();
        var previous = await repository.GetCurrentCatalogItemAsync(type, code, cancellationToken);
        var entity = SharedCatalogItem.Create(type, code, (previous?.Revision ?? 0) + 1,
            request.Name, request.Description, request.UnitRate, request.IsPremium,
            request.Properties.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? "{}" : request.Properties.GetRawText(),
            request.IsDeprecated, request.ActorId);
        await repository.AddCatalogRevisionAsync(entity, previous, cancellationToken);
        return MutationResult<SharedCatalogItemDto>.Success(Map(entity));
    }

    public async Task<MutationResult<bool>> Handle(DeleteSharedCatalogItemCommand request, CancellationToken cancellationToken)
    {
        var type = CanonicalType(request.CatalogType);
        var code = request.Code.Trim().ToUpperInvariant();
        var item = await repository.GetCurrentCatalogItemAsync(type, code, cancellationToken);
        if (item is null) return MutationResult<bool>.Failure("catalog_item_not_found", "Không tìm thấy mục danh mục.");
        if (item.Revision > 1 || await repository.IsCatalogItemInUseAsync(type, code, cancellationToken))
            return MutationResult<bool>.Failure("catalog_item_in_use", "Mục đã được sử dụng hoặc có lịch sử; hãy ngừng sử dụng thay vì xóa.");
        await repository.DeleteCatalogItemAsync(item, cancellationToken);
        return MutationResult<bool>.Success(true);
    }

    private static string CanonicalType(string value) => SharedCatalogTypes.All.First(x => x.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
    private static SharedCatalogItemDto Map(SharedCatalogItem x) => new(
        x.Id, x.CatalogType, x.Code, x.Revision, x.Name, x.Description, x.UnitRate,
        x.IsPremium, JsonDocument.Parse(x.PropertiesJson).RootElement.Clone(), x.IsDeprecated, x.CreatedAtUtc);
}
