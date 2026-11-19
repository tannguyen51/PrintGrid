using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Queries;

public sealed class SchedulingRegistryQueryService(PrintGridDbContext db) : ISchedulingRegistryQueryService
{
    public async Task<IReadOnlyList<LabOptionDto>> GetLabOptionsAsync(CancellationToken cancellationToken = default) =>
        await db.Set<Lab>().AsNoTracking()
            .OrderBy(l => l.Name).ThenBy(l => l.Id)
            .Select(l => new LabOptionDto(l.Id, l.Name, l.City, l.Status.ToString(), l.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<PagedResponse<LabDto>> GetLabsPagedAsync(
        LabPagedQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<Lab>().AsNoTracking();

        if (filter.OwnerUserId.HasValue)
            query = query.Where(l => l.OwnerUserId == filter.OwnerUserId.Value);

        if (!filter.IncludeInactive && string.IsNullOrWhiteSpace(filter.ApprovalStatus) && !filter.IsActive.HasValue)
            query = query.Where(l => l.IsActive && l.Status == LabStatus.Active);

        if (!string.IsNullOrWhiteSpace(filter.ApprovalStatus) &&
            Enum.TryParse<LabStatus>(filter.ApprovalStatus.Trim(), true, out var labStatus))
        {
            query = query.Where(l => l.Status == labStatus);
        }

        if (filter.IsActive.HasValue)
            query = query.Where(l => l.IsActive == filter.IsActive.Value);

        if (filter.IsInGoodStanding.HasValue)
            query = query.Where(l => l.IsInGoodStanding == filter.IsInGoodStanding.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(l =>
                l.Name.ToLower().Contains(term)
                || l.City.ToLower().Contains(term)
                || (l.District != null && l.District.ToLower().Contains(term))
                || (l.Address != null && l.Address.ToLower().Contains(term))
                || (l.ContactPhone != null && l.ContactPhone.ToLower().Contains(term))
                || l.Machines.Any(m =>
                    m.Name.ToLower().Contains(term)
                    || m.Model.ToLower().Contains(term)
                    || (m.PrinterConfiguration != null && m.PrinterConfiguration.Manufacturer.ToLower().Contains(term))));
        }

        if (!string.IsNullOrWhiteSpace(filter.ManufacturerOrModel))
        {
            var hwTerm = filter.ManufacturerOrModel.Trim().ToLowerInvariant();
            query = query.Where(l => l.Machines.Any(m =>
                m.Model.ToLower().Contains(hwTerm)
                || (m.PrinterConfiguration != null && m.PrinterConfiguration.Manufacturer.ToLower().Contains(hwTerm))));
        }

        if (!string.IsNullOrWhiteSpace(filter.Material))
        {
            var mat = filter.Material.Trim().ToUpperInvariant();
            query = query.Where(l => l.Machines.Any(m =>
                EF.Property<List<string>>(m, "_supportedMaterials").Contains(mat)));
        }

        var ordered = (filter.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "name" => query.OrderBy(l => l.Name).ThenBy(l => l.Id),
            "newest" => query.OrderByDescending(l => l.CreatedAt).ThenBy(l => l.Id),
            _ => query
                .OrderBy(l => l.Status == LabStatus.Pending ? 0
                    : l.Status == LabStatus.Active ? 1
                    : l.Status == LabStatus.Rejected ? 2 : 3)
                .ThenByDescending(l => l.CreatedAt)
                .ThenBy(l => l.Name)
                .ThenBy(l => l.Id),
        };

        var totalCount = await ordered.CountAsync(cancellationToken);
        var safePageSize = Math.Clamp(filter.PageSize, 1, 100);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize);
        var safePage = totalPages == 0 ? 1 : Math.Clamp(filter.Page, 1, totalPages);

        if (totalCount == 0)
            return PagedResponse<LabDto>.Create([], 0, safePage, safePageSize);

        var pagedIds = await ordered
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var entities = await db.Set<Lab>().AsNoTracking()
            .Include(l => l.Machines)
            .Include(l => l.MaterialStocks)
            .Where(l => pagedIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        var byId = entities.ToDictionary(l => l.Id);
        var items = pagedIds
            .Where(byId.ContainsKey)
            .Select(id => LabMappings.ToDto(byId[id]))
            .ToList();

        return PagedResponse<LabDto>.Create(items, totalCount, safePage, safePageSize);
    }

    public async Task<PagedResponse<MachineListItemDto>> GetMachinesPagedAsync(
        MachinePagedQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query =
            from m in db.Set<Machine>().AsNoTracking()
            join l in db.Set<Lab>().AsNoTracking() on m.LabId equals l.Id
            select new { Machine = m, Lab = l };

        if (filter.OwnerUserId.HasValue)
            query = query.Where(x => x.Lab.OwnerUserId == filter.OwnerUserId.Value);

        if (filter.LabId.HasValue)
            query = query.Where(x => x.Machine.LabId == filter.LabId.Value);

        if (!string.IsNullOrWhiteSpace(filter.ProfileStatus))
        {
            var profileStatus = filter.ProfileStatus.Trim();
            query = query.Where(x => x.Machine.ProfileStatus == profileStatus);
        }

        if (!string.IsNullOrWhiteSpace(filter.OperationalStatus) &&
            Enum.TryParse<MachineStatus>(filter.OperationalStatus.Trim(), true, out var opStatus))
        {
            query = query.Where(x => x.Machine.Status == opStatus);
        }

        if (!string.IsNullOrWhiteSpace(filter.LabStatus) &&
            Enum.TryParse<LabStatus>(filter.LabStatus.Trim(), true, out var labStatus))
        {
            query = query.Where(x => x.Lab.Status == labStatus);
        }

        if (!string.IsNullOrWhiteSpace(filter.Technology) &&
            Enum.TryParse<PrintTechnology>(filter.Technology.Trim(), true, out var tech))
        {
            query = query.Where(x => x.Machine.Technology == tech);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Machine.Name.ToLower().Contains(term)
                || x.Machine.Model.ToLower().Contains(term)
                || (x.Machine.PrinterConfiguration != null && x.Machine.PrinterConfiguration.Manufacturer.ToLower().Contains(term))
                || x.Lab.Name.ToLower().Contains(term)
                || x.Lab.City.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.ManufacturerOrModel))
        {
            var hwTerm = filter.ManufacturerOrModel.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Machine.Model.ToLower().Contains(hwTerm)
                || (x.Machine.PrinterConfiguration != null && x.Machine.PrinterConfiguration.Manufacturer.ToLower().Contains(hwTerm)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Material))
        {
            var mat = filter.Material.Trim().ToUpperInvariant();
            query = query.Where(x => EF.Property<List<string>>(x.Machine, "_supportedMaterials").Contains(mat));
        }

        var ordered = (filter.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "name" => query
                .OrderBy(x => x.Lab.Name)
                .ThenBy(x => x.Machine.Name)
                .ThenBy(x => x.Machine.Id),
            _ => query
                .OrderBy(x => x.Machine.ProfileStatus == "PendingReview" ? 0
                    : x.Machine.ProfileStatus == "MissingProfile" ? 1
                    : x.Machine.ProfileStatus == "Rejected" ? 2 : 3)
                .ThenBy(x => x.Lab.Name)
                .ThenBy(x => x.Machine.Name)
                .ThenBy(x => x.Machine.Id),
        };

        var totalCount = await ordered.CountAsync(cancellationToken);
        var safePageSize = Math.Clamp(filter.PageSize, 1, 100);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize);
        var safePage = totalPages == 0 ? 1 : Math.Clamp(filter.Page, 1, totalPages);

        if (totalCount == 0)
            return PagedResponse<MachineListItemDto>.Create([], 0, safePage, safePageSize);

        var rows = await ordered
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var profileIds = rows
            .SelectMany(x => x.Machine.CuraProfileIds)
            .Distinct()
            .ToArray();

        var profileMap = profileIds.Length == 0
            ? new Dictionary<Guid, MachineMatchedProfileSummaryDto>()
            : await db.Set<CuraProfile>().AsNoTracking()
                .Where(p => profileIds.Contains(p.Id))
                .Select(p => new MachineMatchedProfileSummaryDto(
                    p.Id, p.Name, p.MaterialCode, p.Version, p.ApprovalStatus, p.IsActive))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

        var items = rows.Select(x =>
        {
            var m = x.Machine;
            var lab = x.Lab;
            var matched = m.CuraProfileIds
                .Where(profileMap.ContainsKey)
                .Select(id => profileMap[id])
                .ToList();

            var eligible = lab.IsActive
                && lab.Status == LabStatus.Active
                && lab.IsInGoodStanding
                && m.Status is MachineStatus.Idle or MachineStatus.Printing
                && (m.Technology != PrintTechnology.Fdm || (m.ProfileStatus == "Approved" && m.CuraProfileIds.Length > 0));

            return new MachineListItemDto(
                m.Id,
                lab.Id,
                lab.Name,
                lab.City,
                lab.Status.ToString(),
                lab.IsActive,
                lab.IsInGoodStanding,
                m.Name,
                m.Model,
                m.Technology,
                m.BuildVolume.WidthMm,
                m.BuildVolume.DepthMm,
                m.BuildVolume.HeightMm,
                m.MinLayerHeightMm,
                m.AchievableToleranceMm,
                m.SpeedFactor,
                m.Status,
                [.. m.SupportedMaterials],
                [.. m.CuraProfileIds],
                matched,
                m.MaintenanceFromUtc,
                m.MaintenanceUntilUtc,
                m.MaintenanceReason,
                m.PrinterConfiguration,
                m.ProfileStatus,
                m.ProfileReviewReason,
                m.ProfileReviewedAtUtc,
                eligible);
        }).ToList();

        return PagedResponse<MachineListItemDto>.Create(items, totalCount, safePage, safePageSize);
    }

    public async Task<PagedResponse<CuraProfileListItemDto>> GetCuraProfilesPagedAsync(
        CuraProfilePagedQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<CuraProfile>().AsNoTracking();
        var scope = filter.Scope?.Trim().ToLowerInvariant() ?? "all";

        if (scope == "library")
            query = query.Where(p => p.ApprovalStatus == CuraProfile.StatusApproved);
        else if (scope == "proposals")
            query = query.Where(p => p.SubmittedByLabId != null);

        if (filter.LabId.HasValue)
            query = query.Where(p => p.SubmittedByLabId == filter.LabId.Value);

        if (!string.IsNullOrWhiteSpace(filter.ApprovalStatus))
        {
            var status = filter.ApprovalStatus.Trim();
            query = query.Where(p => p.ApprovalStatus == status);
        }

        if (filter.IsActive.HasValue)
            query = query.Where(p => p.IsActive == filter.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(filter.ManufacturerOrModel))
        {
            var hwTerm = filter.ManufacturerOrModel.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Manufacturer.ToLower().Contains(hwTerm)
                || p.MachineModel.ToLower().Contains(hwTerm));
        }

        if (!string.IsNullOrWhiteSpace(filter.Material))
        {
            var matTerm = filter.Material.Trim().ToLowerInvariant();
            query = query.Where(p => p.MaterialCode.ToLower().Contains(matTerm));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term)
                || p.Manufacturer.ToLower().Contains(term)
                || p.MachineModel.ToLower().Contains(term)
                || p.MaterialCode.ToLower().Contains(term)
                || p.Version.ToLower().Contains(term)
                || (p.SourceFileName != null && p.SourceFileName.ToLower().Contains(term))
                || (p.SubmissionNotes != null && p.SubmissionNotes.ToLower().Contains(term))
                || (p.SubmittedByLabId != null && db.Set<Lab>().Any(l => l.Id == p.SubmittedByLabId.Value && l.Name.ToLower().Contains(term))));
        }

        var ordered = scope == "proposals"
            ? query
                .OrderBy(p => p.ApprovalStatus == CuraProfile.StatusPendingReview ? 0
                    : p.ApprovalStatus == CuraProfile.StatusRejected ? 1 : 2)
                .ThenByDescending(p => p.UpdatedAtUtc)
                .ThenBy(p => p.Id)
            : query
                .OrderBy(p => p.Manufacturer)
                .ThenBy(p => p.MachineModel)
                .ThenBy(p => p.MaterialCode)
                .ThenByDescending(p => p.CreatedAtUtc)
                .ThenBy(p => p.Id);

        var totalCount = await ordered.CountAsync(cancellationToken);
        var safePageSize = Math.Clamp(filter.PageSize, 1, 100);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize);
        var safePage = totalPages == 0 ? 1 : Math.Clamp(filter.Page, 1, totalPages);

        if (totalCount == 0)
            return PagedResponse<CuraProfileListItemDto>.Create([], 0, safePage, safePageSize);

        var pageProfiles = await ordered
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(p => new CuraProfileRow(
                p.Id, p.Name, p.Manufacturer, p.MachineModel, p.MaterialCode, p.Version, p.EngineVersion,
                p.NozzleDiameterMm, p.FilamentDiameterMm, p.ExtruderCount, p.MaximumColorSlots,
                p.WidthMm, p.DepthMm, p.HeightMm, p.HeatedBed, p.RequiredNozzleTemperatureC, p.RequiredBedTemperatureC,
                p.IsActive, p.IsVerified, p.ApprovalStatus, p.SubmittedByLabId, p.TargetMachineId,
                p.SubmissionNotes, p.SourceFileName, p.ReviewReason, p.ReviewedAtUtc, p.CreatedAtUtc, p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        var labIds = pageProfiles.Where(p => p.SubmittedByLabId.HasValue).Select(p => p.SubmittedByLabId!.Value).Distinct().ToArray();
        var machineIds = pageProfiles.Where(p => p.TargetMachineId.HasValue).Select(p => p.TargetMachineId!.Value).Distinct().ToArray();

        var labNames = labIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await db.Set<Lab>().AsNoTracking().Where(l => labIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => l.Name, cancellationToken);

        var machineNames = machineIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await db.Set<Machine>().AsNoTracking().Where(m => machineIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => $"{m.Name} ({m.Model})", cancellationToken);

        var items = pageProfiles.Select(p => new CuraProfileListItemDto(
            p.Id, p.Name, p.Manufacturer, p.MachineModel, p.MaterialCode, p.Version, p.EngineVersion,
            p.NozzleDiameterMm, p.FilamentDiameterMm, p.ExtruderCount, p.MaximumColorSlots,
            p.WidthMm, p.DepthMm, p.HeightMm, p.HeatedBed, p.RequiredNozzleTemperatureC, p.RequiredBedTemperatureC,
            p.IsActive, p.IsVerified, p.ApprovalStatus,
            p.SubmittedByLabId,
            p.SubmittedByLabId is { } labId ? labNames.GetValueOrDefault(labId) : null,
            p.TargetMachineId,
            p.TargetMachineId is { } machineId ? machineNames.GetValueOrDefault(machineId) : null,
            p.SubmissionNotes, p.SourceFileName, p.ReviewReason, p.ReviewedAtUtc, p.CreatedAtUtc, p.UpdatedAtUtc))
            .ToList();

        return PagedResponse<CuraProfileListItemDto>.Create(items, totalCount, safePage, safePageSize);
    }

    public async Task<PagedResponse<CuraProfileListItemDto>> GetMyCuraProfileProposalsAsync(
        Guid callerUserId,
        MyCuraProfileProposalsFilter filter,
        CancellationToken cancellationToken = default)
    {
        var labId = await db.Set<Lab>().AsNoTracking()
            .Where(l => l.OwnerUserId == callerUserId || l.Staff.Any(s => s.UserId == callerUserId))
            .Select(l => (Guid?)l.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (labId is null)
            return PagedResponse<CuraProfileListItemDto>.Create([], 0, 1, Math.Clamp(filter.PageSize, 1, 100));

        return await GetCuraProfilesPagedAsync(
            new CuraProfilePagedQueryFilter(
                Scope: "proposals",
                Search: filter.Search,
                LabId: labId.Value,
                ApprovalStatus: filter.ApprovalStatus,
                Material: filter.Material,
                Page: filter.Page,
                PageSize: filter.PageSize),
            cancellationToken);
    }

    public async Task<CuraProfileDetailDto?> GetCuraProfileDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await db.Set<CuraProfile>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (p is null) return null;

        var labName = p.SubmittedByLabId is { } labId
            ? await db.Set<Lab>().AsNoTracking().Where(l => l.Id == labId).Select(l => l.Name).SingleOrDefaultAsync(cancellationToken)
            : null;
        var machineName = p.TargetMachineId is { } machineId
            ? await db.Set<Machine>().AsNoTracking().Where(m => m.Id == machineId).Select(m => $"{m.Name} ({m.Model})").SingleOrDefaultAsync(cancellationToken)
            : null;

        return new CuraProfileDetailDto(
            p.Id, p.Name, p.Manufacturer, p.MachineModel, p.MaterialCode, p.Version, p.EngineVersion,
            p.NozzleDiameterMm, p.FilamentDiameterMm, p.ExtruderCount, p.MaximumColorSlots,
            p.WidthMm, p.DepthMm, p.HeightMm, p.HeatedBed, p.RequiredNozzleTemperatureC, p.RequiredBedTemperatureC,
            p.SettingsJson, p.IsActive, p.IsVerified, p.ApprovalStatus,
            p.SubmittedByLabId, labName, p.TargetMachineId, machineName,
            p.SubmissionNotes, p.SourceFileName, p.ReviewReason, p.ReviewedAtUtc, p.CreatedAtUtc, p.UpdatedAtUtc);
    }

    private sealed record CuraProfileRow(
        Guid Id, string Name, string Manufacturer, string MachineModel, string MaterialCode, string Version, string EngineVersion,
        decimal NozzleDiameterMm, decimal FilamentDiameterMm, int ExtruderCount, int MaximumColorSlots,
        decimal WidthMm, decimal DepthMm, decimal HeightMm, bool HeatedBed, decimal RequiredNozzleTemperatureC, decimal RequiredBedTemperatureC,
        bool IsActive, bool IsVerified, string ApprovalStatus, Guid? SubmittedByLabId, Guid? TargetMachineId,
        string? SubmissionNotes, string? SourceFileName, string? ReviewReason, DateTime? ReviewedAtUtc, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
}
