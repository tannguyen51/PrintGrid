using System.Text.Json;
using FluentValidation;
using MediatR;
using PrintGrid.Modules.Admin.Application.Configuration;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;

namespace PrintGrid.Modules.Admin.Application.QcKnowledge;

public sealed record InspectionChecklistItemDto(
    string Code, string Name, string PassCriteria, bool IsMandatory, int DisplayOrder);
public sealed record InspectionChecklistVersionDto(
    Guid Id, string QualityGradeCode, int VersionNumber,
    IReadOnlyList<InspectionChecklistItemDto> Items, bool IsActive,
    Guid CreatedBy, DateTime CreatedAtUtc);
public sealed record DefectTaxonomyItemDto(
    Guid Id, string Code, string Name, string? Description,
    string? DefaultFaultAttribution, bool IsDeprecated,
    Guid CreatedBy, DateTime CreatedAtUtc, Guid? UpdatedBy, DateTime? UpdatedAtUtc);
public sealed record QcKnowledgeDto(
    InspectionChecklistVersionDto Checklist,
    IReadOnlyList<DefectTaxonomyItemDto> Defects);

public sealed record GetInspectionChecklistsQuery(string? QualityGradeCode)
    : IRequest<IReadOnlyList<InspectionChecklistVersionDto>>;
public sealed record CreateInspectionChecklistVersionCommand(
    string QualityGradeCode, IReadOnlyList<InspectionChecklistItemDto> Items, Guid ActorId)
    : IRequest<MutationResult<InspectionChecklistVersionDto>>;
public sealed record GetDefectTaxonomyQuery(bool IncludeDeprecated)
    : IRequest<IReadOnlyList<DefectTaxonomyItemDto>>;
public sealed record SaveDefectTaxonomyItemCommand(
    string Code, string Name, string? Description, string? DefaultFaultAttribution,
    bool IsDeprecated, Guid ActorId) : IRequest<MutationResult<DefectTaxonomyItemDto>>;
public sealed record DeleteDefectTaxonomyItemCommand(string Code) : IRequest<MutationResult<bool>>;
public sealed record GetQcKnowledgeQuery(decimal LayerHeightMm) : IRequest<MutationResult<QcKnowledgeDto>>;

public sealed class InspectionChecklistItemValidator : AbstractValidator<InspectionChecklistItemDto>
{
    public InspectionChecklistItemValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.PassCriteria).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DisplayOrder).GreaterThan(0);
    }
}

public sealed class CreateInspectionChecklistVersionValidator : AbstractValidator<CreateInspectionChecklistVersionCommand>
{
    public CreateInspectionChecklistVersionValidator()
    {
        RuleFor(x => x.QualityGradeCode).NotEmpty().MaximumLength(32).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().Must(x => x.Count <= 50).WithMessage("Bộ kiểm có tối đa 50 tiêu chí.");
        RuleForEach(x => x.Items).SetValidator(new InspectionChecklistItemValidator());
        RuleFor(x => x.Items).Must(items => items.Select(i => i.Code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == items.Count)
            .WithMessage("Mã tiêu chí không được trùng.");
        RuleFor(x => x.Items).Must(items => items.Select(i => i.DisplayOrder).Distinct().Count() == items.Count)
            .WithMessage("Thứ tự tiêu chí không được trùng.");
    }
}

public sealed class SaveDefectTaxonomyItemValidator : AbstractValidator<SaveDefectTaxonomyItemCommand>
{
    private static readonly string[] Faults = ["Lab", "Hub", "Customer"];
    public SaveDefectTaxonomyItemValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.DefaultFaultAttribution).Must(x => string.IsNullOrWhiteSpace(x) || Faults.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Quy trách nhiệm mặc định phải là Lab, Hub hoặc Customer.");
    }
}

public sealed class QcKnowledgeHandlers(IQcKnowledgeRepository repository) :
    IRequestHandler<GetInspectionChecklistsQuery, IReadOnlyList<InspectionChecklistVersionDto>>,
    IRequestHandler<CreateInspectionChecklistVersionCommand, MutationResult<InspectionChecklistVersionDto>>,
    IRequestHandler<GetDefectTaxonomyQuery, IReadOnlyList<DefectTaxonomyItemDto>>,
    IRequestHandler<SaveDefectTaxonomyItemCommand, MutationResult<DefectTaxonomyItemDto>>,
    IRequestHandler<DeleteDefectTaxonomyItemCommand, MutationResult<bool>>,
    IRequestHandler<GetQcKnowledgeQuery, MutationResult<QcKnowledgeDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<InspectionChecklistVersionDto>> Handle(GetInspectionChecklistsQuery request, CancellationToken cancellationToken) =>
        (await repository.GetChecklistVersionsAsync(NormalizeOptional(request.QualityGradeCode), cancellationToken)).Select(Map).ToList();

    public async Task<MutationResult<InspectionChecklistVersionDto>> Handle(CreateInspectionChecklistVersionCommand request, CancellationToken cancellationToken)
    {
        var grade = request.QualityGradeCode.Trim().ToUpperInvariant();
        if (!await repository.QualityGradeExistsAsync(grade, cancellationToken))
            return MutationResult<InspectionChecklistVersionDto>.Failure("quality_grade_not_found", "Cấp chất lượng không tồn tại hoặc đã ngừng sử dụng.");
        var previous = await repository.GetActiveChecklistAsync(grade, cancellationToken);
        var versionNumber = await repository.GetNextChecklistVersionAsync(grade, cancellationToken);
        var normalizedItems = request.Items.OrderBy(x => x.DisplayOrder)
            .Select(x => x with { Code = x.Code.Trim().ToUpperInvariant(), Name = x.Name.Trim(), PassCriteria = x.PassCriteria.Trim() }).ToList();
        var version = InspectionChecklistVersion.Create(grade, versionNumber,
            JsonSerializer.Serialize(normalizedItems, JsonOptions), request.ActorId);
        await repository.AddChecklistVersionAsync(version, previous, cancellationToken);
        return MutationResult<InspectionChecklistVersionDto>.Success(Map(version));
    }

    public async Task<IReadOnlyList<DefectTaxonomyItemDto>> Handle(GetDefectTaxonomyQuery request, CancellationToken cancellationToken) =>
        (await repository.GetDefectsAsync(request.IncludeDeprecated, cancellationToken)).Select(Map).ToList();

    public async Task<MutationResult<DefectTaxonomyItemDto>> Handle(SaveDefectTaxonomyItemCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var item = await repository.GetDefectAsync(code, cancellationToken);
        if (item is null)
        {
            item = DefectTaxonomyItem.Create(code, request.Name, request.Description,
                CanonicalFault(request.DefaultFaultAttribution), request.IsDeprecated, request.ActorId);
            await repository.AddDefectAsync(item, cancellationToken);
        }
        else
        {
            item.Update(request.Name, request.Description, CanonicalFault(request.DefaultFaultAttribution),
                request.IsDeprecated, request.ActorId);
            await repository.SaveDefectAsync(cancellationToken);
        }
        return MutationResult<DefectTaxonomyItemDto>.Success(Map(item));
    }

    public async Task<MutationResult<bool>> Handle(DeleteDefectTaxonomyItemCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var item = await repository.GetDefectAsync(code, cancellationToken);
        if (item is null) return MutationResult<bool>.Failure("defect_not_found", "Không tìm thấy mã lỗi.");
        if (await repository.IsDefectInUseAsync(code, cancellationToken))
            return MutationResult<bool>.Failure("defect_in_use", "Mã lỗi đã có trong lịch sử kiểm định; chỉ được ngừng sử dụng.");
        await repository.DeleteDefectAsync(item, cancellationToken);
        return MutationResult<bool>.Success(true);
    }

    public async Task<MutationResult<QcKnowledgeDto>> Handle(GetQcKnowledgeQuery request, CancellationToken cancellationToken)
    {
        var grade = request.LayerHeightMm <= 0.1m ? "FINE" : "STANDARD";
        var checklist = await repository.GetActiveChecklistAsync(grade, cancellationToken);
        if (checklist is null)
            return MutationResult<QcKnowledgeDto>.Failure("active_checklist_not_found", "Chưa có bộ kiểm đang hoạt động cho cấp chất lượng này.");
        var defects = await repository.GetDefectsAsync(false, cancellationToken);
        return MutationResult<QcKnowledgeDto>.Success(new(Map(checklist), defects.Select(Map).ToList()));
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static string? CanonicalFault(string? value) => string.IsNullOrWhiteSpace(value)
        ? null : char.ToUpperInvariant(value.Trim()[0]) + value.Trim()[1..].ToLowerInvariant();
    private static InspectionChecklistVersionDto Map(InspectionChecklistVersion x) => new(
        x.Id, x.QualityGradeCode, x.VersionNumber,
        JsonSerializer.Deserialize<List<InspectionChecklistItemDto>>(x.ItemsJson, JsonOptions) ?? [],
        x.IsActive, x.CreatedBy, x.CreatedAtUtc);
    private static DefectTaxonomyItemDto Map(DefectTaxonomyItem x) => new(
        x.Id, x.Code, x.Name, x.Description, x.DefaultFaultAttribution, x.IsDeprecated,
        x.CreatedBy, x.CreatedAtUtc, x.UpdatedBy, x.UpdatedAtUtc);
}
