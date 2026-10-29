using MediatR;
using PrintGrid.Modules.Admin.Application.Configuration;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Admin.Application.Samples;

// ── Public reads (anonymous — see SamplesController) ──────────────────────────

public sealed record GetPublicSamplesQuery(string? Search, string? Category)
    : IRequest<IReadOnlyList<SampleSummaryDto>>;

public sealed record GetPublicSampleQuery(string Slug)
    : IRequest<Result<SampleDetailDto>>;

// ── Admin curation (RequireAdmin) ─────────────────────────────────────────────

public sealed record GetAdminSamplesQuery : IRequest<IReadOnlyList<SampleAdminDto>>;

/// <summary>
/// Internal read used by the copy-on-order endpoint: it needs the stored file location,
/// which the public DTO deliberately never exposes. The endpoint is Customer-only.
/// </summary>
public sealed record GetSampleSourceQuery(string Slug) : IRequest<Result<SampleAdminDto>>;

public sealed record GetSampleByIdQuery(Guid Id) : IRequest<Result<SampleAdminDto>>;

public sealed record CreateSampleCommand(
    string? Slug,
    string Name,
    string? Description,
    string Category,
    string FileName,
    string FileFormat,
    long SizeBytes,
    string StorageKey,
    string? Sha256,
    string? CoverImageUrl,
    IReadOnlyList<string>? Tags,
    int SortOrder,
    bool IsPublished,
    decimal WidthMm,
    decimal DepthMm,
    decimal HeightMm,
    decimal VolumeCm3,
    int EstimatedPrintMinutes,
    decimal EstimatedMaterialGrams,
    Guid ActorId) : IRequest<MutationResult<SampleAdminDto>>;

public sealed record UpdateSampleCommand(
    Guid Id,
    string? Slug,
    string Name,
    string? Description,
    string Category,
    string? CoverImageUrl,
    IReadOnlyList<string>? Tags,
    int SortOrder,
    // Optional replacement file (admins can swap the binary without losing the slug).
    string? FileName,
    string? FileFormat,
    long? SizeBytes,
    string? StorageKey,
    string? Sha256,
    decimal? WidthMm,
    decimal? DepthMm,
    decimal? HeightMm,
    decimal? VolumeCm3,
    int? EstimatedPrintMinutes,
    decimal? EstimatedMaterialGrams,
    Guid ActorId) : IRequest<MutationResult<SampleAdminDto>>;

public sealed record SetSamplePublishedCommand(Guid Id, bool Published)
    : IRequest<MutationResult<SampleAdminDto>>;

public sealed record DeleteSampleCommand(Guid Id) : IRequest<MutationResult<bool>>;

public sealed class SampleRequestHandlers(ISampleModelRepository repository, ICommercialPolicyProvider pricing) :
    IRequestHandler<GetPublicSamplesQuery, IReadOnlyList<SampleSummaryDto>>,
    IRequestHandler<GetPublicSampleQuery, Result<SampleDetailDto>>,
    IRequestHandler<GetAdminSamplesQuery, IReadOnlyList<SampleAdminDto>>,
    IRequestHandler<GetSampleSourceQuery, Result<SampleAdminDto>>,
    IRequestHandler<GetSampleByIdQuery, Result<SampleAdminDto>>,
    IRequestHandler<CreateSampleCommand, MutationResult<SampleAdminDto>>,
    IRequestHandler<UpdateSampleCommand, MutationResult<SampleAdminDto>>,
    IRequestHandler<SetSamplePublishedCommand, MutationResult<SampleAdminDto>>,
    IRequestHandler<DeleteSampleCommand, MutationResult<bool>>
{
    public async Task<IReadOnlyList<SampleSummaryDto>> Handle(GetPublicSamplesQuery request, CancellationToken cancellationToken)
    {
        var set = (await pricing.GetActiveAsync(cancellationToken)).Pricing;
        var samples = await repository.GetPublishedAsync(request.Search, request.Category, cancellationToken);
        return samples.Select(sample => SampleMappings.ToSummary(sample, set)).ToList();
    }

    public async Task<Result<SampleDetailDto>> Handle(GetPublicSampleQuery request, CancellationToken cancellationToken)
    {
        var sample = await repository.GetPublishedBySlugAsync(request.Slug.Trim(), cancellationToken);
        if (sample is null)
            return Result.Failure<SampleDetailDto>(Error.NotFound("SampleModel", request.Slug));

        var set = (await pricing.GetActiveAsync(cancellationToken)).Pricing;
        return Result.Success(SampleMappings.ToDetail(sample, set));
    }

    public async Task<IReadOnlyList<SampleAdminDto>> Handle(GetAdminSamplesQuery request, CancellationToken cancellationToken) =>
        (await repository.GetAllAsync(cancellationToken)).Select(SampleMappings.ToAdmin).ToList();

    public async Task<Result<SampleAdminDto>> Handle(GetSampleSourceQuery request, CancellationToken cancellationToken)
    {
        var sample = await repository.GetBySlugAsync(request.Slug.Trim(), cancellationToken);
        return sample is { IsPublished: true }
            ? Result.Success(SampleMappings.ToAdmin(sample))
            : Result.Failure<SampleAdminDto>(Error.NotFound("SampleModel", request.Slug));
    }

    public async Task<Result<SampleAdminDto>> Handle(GetSampleByIdQuery request, CancellationToken cancellationToken)
    {
        var sample = await repository.GetByIdAsync(request.Id, cancellationToken);
        return sample is null
            ? Result.Failure<SampleAdminDto>(Error.NotFound("SampleModel", request.Id))
            : Result.Success(SampleMappings.ToAdmin(sample));
    }

    public async Task<MutationResult<SampleAdminDto>> Handle(CreateSampleCommand request, CancellationToken cancellationToken)
    {
        var slug = await ResolveSlugAsync(request.Slug, request.Name, null, cancellationToken);
        if (slug.Error is not null) return MutationResult<SampleAdminDto>.Failure(slug.Error, "Slug đã được dùng cho một mẫu khác.");

        var sample = SampleModel.Create(
            slug.Value!, request.Name, request.Description, request.Category,
            request.FileName, request.FileFormat, request.SizeBytes, request.StorageKey,
            request.Sha256, request.CoverImageUrl, request.Tags,
            request.SortOrder, request.IsPublished, request.ActorId);

        sample.ApplyGeometry(
            request.WidthMm, request.DepthMm, request.HeightMm, request.VolumeCm3,
            request.EstimatedPrintMinutes, request.EstimatedMaterialGrams);

        await repository.AddAsync(sample, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return MutationResult<SampleAdminDto>.Success(SampleMappings.ToAdmin(sample));
    }

    public async Task<MutationResult<SampleAdminDto>> Handle(UpdateSampleCommand request, CancellationToken cancellationToken)
    {
        var sample = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (sample is null)
            return MutationResult<SampleAdminDto>.Failure("sample_not_found", "Không tìm thấy mẫu.");

        var slug = await ResolveSlugAsync(request.Slug, request.Name, request.Id, cancellationToken);
        if (slug.Error is not null) return MutationResult<SampleAdminDto>.Failure(slug.Error, "Slug đã được dùng cho một mẫu khác.");

        sample.UpdateDetails(
            slug.Value!, request.Name, request.Description, request.Category,
            request.CoverImageUrl, request.Tags, request.SortOrder);

        if (!string.IsNullOrWhiteSpace(request.StorageKey))
        {
            sample.ReplaceFile(
                request.FileName ?? sample.FileName,
                request.FileFormat ?? sample.FileFormat,
                request.SizeBytes ?? sample.SizeBytes,
                request.StorageKey,
                request.Sha256);
        }

        if (request.VolumeCm3 is { } volume && request.WidthMm is { } width && request.DepthMm is { } depth
            && request.HeightMm is { } height && request.EstimatedPrintMinutes is { } minutes
            && request.EstimatedMaterialGrams is { } grams)
        {
            sample.ApplyGeometry(width, depth, height, volume, minutes, grams);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return MutationResult<SampleAdminDto>.Success(SampleMappings.ToAdmin(sample));
    }

    public async Task<MutationResult<SampleAdminDto>> Handle(SetSamplePublishedCommand request, CancellationToken cancellationToken)
    {
        var sample = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (sample is null)
            return MutationResult<SampleAdminDto>.Failure("sample_not_found", "Không tìm thấy mẫu.");

        sample.SetPublished(request.Published);
        await repository.SaveChangesAsync(cancellationToken);
        return MutationResult<SampleAdminDto>.Success(SampleMappings.ToAdmin(sample));
    }

    public async Task<MutationResult<bool>> Handle(DeleteSampleCommand request, CancellationToken cancellationToken)
    {
        var sample = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (sample is null)
            return MutationResult<bool>.Failure("sample_not_found", "Không tìm thấy mẫu.");

        repository.Remove(sample);
        await repository.SaveChangesAsync(cancellationToken);
        return MutationResult<bool>.Success(true);
    }

    /// <summary>
    /// Auto-generated slugs are suffixed until free (-2, -3 …) so two samples named
    /// "Hộp bút" can coexist; an explicitly typed slug that is taken is an error.
    /// </summary>
    private async Task<(string? Value, string? Error)> ResolveSlugAsync(
        string? requestedSlug, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var baseSlug = SampleModel.NormaliseSlug(requestedSlug, name);

        if (!string.IsNullOrWhiteSpace(requestedSlug))
            return await repository.SlugExistsAsync(baseSlug, exceptId, cancellationToken)
                ? (null, "slug_exists")
                : (baseSlug, null);

        var candidate = baseSlug;
        for (var suffix = 2; await repository.SlugExistsAsync(candidate, exceptId, cancellationToken); suffix++)
            candidate = $"{baseSlug}-{suffix}";

        return (candidate, null);
    }
}
