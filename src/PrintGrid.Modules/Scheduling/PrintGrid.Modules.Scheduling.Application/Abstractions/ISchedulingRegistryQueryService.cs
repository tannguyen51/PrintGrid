using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Abstractions;

public sealed record LabPagedQueryFilter(
    string? Search = null,
    string? ApprovalStatus = null,
    bool? IsActive = null,
    bool? IsInGoodStanding = null,
    string? ManufacturerOrModel = null,
    string? Material = null,
    bool IncludeInactive = true,
    string? SortBy = "pendingFirst",
    Guid? OwnerUserId = null,
    int Page = 1,
    int PageSize = 10);

public sealed record MachinePagedQueryFilter(
    string? Search = null,
    Guid? LabId = null,
    string? ProfileStatus = null,
    string? OperationalStatus = null,
    string? LabStatus = null,
    string? ManufacturerOrModel = null,
    string? Material = null,
    string? Technology = null,
    string? SortBy = "pendingFirst",
    Guid? OwnerUserId = null,
    int Page = 1,
    int PageSize = 10);

public sealed record CuraProfilePagedQueryFilter(
    string? Scope = "all",
    string? Search = null,
    Guid? LabId = null,
    string? ApprovalStatus = null,
    bool? IsActive = null,
    string? ManufacturerOrModel = null,
    string? Material = null,
    int Page = 1,
    int PageSize = 10);

public sealed record MyCuraProfileProposalsFilter(
    string? Search = null,
    string? ApprovalStatus = null,
    string? Material = null,
    int Page = 1,
    int PageSize = 10);

public interface ISchedulingRegistryQueryService
{
    Task<IReadOnlyList<LabOptionDto>> GetLabOptionsAsync(CancellationToken cancellationToken = default);
    Task<PagedResponse<LabDto>> GetLabsPagedAsync(LabPagedQueryFilter filter, CancellationToken cancellationToken = default);
    Task<PagedResponse<MachineListItemDto>> GetMachinesPagedAsync(MachinePagedQueryFilter filter, CancellationToken cancellationToken = default);
    Task<PagedResponse<CuraProfileListItemDto>> GetCuraProfilesPagedAsync(CuraProfilePagedQueryFilter filter, CancellationToken cancellationToken = default);
    Task<PagedResponse<CuraProfileListItemDto>> GetMyCuraProfileProposalsAsync(Guid callerUserId, MyCuraProfileProposalsFilter filter, CancellationToken cancellationToken = default);
    Task<CuraProfileDetailDto?> GetCuraProfileDetailAsync(Guid id, CancellationToken cancellationToken = default);
}
