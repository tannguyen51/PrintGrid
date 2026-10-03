using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Api.BackgroundJobs;

/// <summary>
/// Runs after a model is uploaded: analyzes the geometry (FR-SCHED-001) and produces the
/// reference print-time cutoff (FR-SCHED-002) that quoting consumes.
/// While binary upload is metadata-only, the analysis derives a deterministic placeholder
/// envelope; wiring MinIO replaces AnalyzeFromMetadata with AnalyzeAsync on the real bytes.
/// </summary>
public class AnalyzeModelJob
{
    private readonly IModelRepository _models;
    private readonly ISlicingService _slicing;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _files;
    private readonly ILabRepository _labs;
    private readonly ILogger<AnalyzeModelJob> _logger;

    public AnalyzeModelJob(
        IModelRepository models,
        ISlicingService slicing,
        IUnitOfWork unitOfWork,
        IFileStorage files,
        ILabRepository labs,
        ILogger<AnalyzeModelJob> logger)
    {
        _models = models;
        _slicing = slicing;
        _unitOfWork = unitOfWork;
        _files = files;
        _labs = labs;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var model = await _models.GetByIdAsync(modelId, cancellationToken);
        if (model is null)
        {
            _logger.LogWarning("AnalyzeModelJob: model {ModelId} not found", modelId);
            return;
        }

        try
        {
            // Default reference configuration — the quote flow re-slices per customer config.
            GeometryAnalysis geometry;
            if (!string.IsNullOrWhiteSpace(model.StorageKey))
            {
                var separator = model.StorageKey.IndexOf('/');
                if (separator <= 0 || separator == model.StorageKey.Length - 1)
                    throw new InvalidDataException("Invalid model storage key");

                await using var stream = await _files.DownloadAsync(
                    model.StorageKey[..separator], model.StorageKey[(separator + 1)..], cancellationToken);
                geometry = await _slicing.AnalyzeAsync(stream, model.FileFormat, cancellationToken);
            }
            else
            {
                geometry = _slicing.AnalyzeFromMetadata(model.SizeBytes);
            }

            if (!geometry.IsValid)
                throw new InvalidDataException(geometry.ErrorMessage ?? "Geometry analysis failed");

            var estimate = _slicing.Estimate(geometry, "PLA", 0.2m, 30, PrintTechnology.Fdm);
            var labs = await _labs.GetActiveWithMachinesAsync(cancellationToken);
            var fitsAnyMachine = labs.SelectMany(lab => lab.Machines).Any(machine =>
                CanFitInAnyOrientation(
                    geometry.WidthMm, geometry.DepthMm, geometry.HeightMm,
                    machine.BuildVolume.WidthMm, machine.BuildVolume.DepthMm, machine.BuildVolume.HeightMm));
            var issues = new List<string>();
            if (!geometry.IsWatertight) issues.Add("Model không kín (not watertight)");
            if (!geometry.IsManifold) issues.Add("Model có cạnh non-manifold");
            if (!fitsAnyMachine) issues.Add("Kích thước model vượt quá khổ in của tất cả máy trong mạng lưới");
            var printable = geometry.IsWatertight && geometry.IsManifold && fitsAnyMachine;

            model.ApplyGeometry(
                geometry.WidthMm,
                geometry.DepthMm,
                geometry.HeightMm,
                geometry.VolumeCm3,
                estimate.PrintMinutes,
                estimate.MaterialGrams,
                geometry.IsWatertight,
                geometry.IsManifold,
                printable,
                issues.Count == 0 ? null : string.Join("; ", issues));

            _logger.LogInformation(
                "Analyzed model {ModelId}: {Volume} cm3, {Minutes} min reference estimate",
                model.Id, geometry.VolumeCm3, estimate.PrintMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AnalyzeModelJob failed for model {ModelId}", model.Id);
            model.MarkGeometryFailed(ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static bool CanFitInAnyOrientation(
        decimal partWidth, decimal partDepth, decimal partHeight,
        decimal bedWidth, decimal bedDepth, decimal bedHeight)
    {
        var part = new[] { partWidth, partDepth, partHeight }.OrderBy(value => value).ToArray();
        var bed = new[] { bedWidth, bedDepth, bedHeight }.OrderBy(value => value).ToArray();
        return part[0] <= bed[0] && part[1] <= bed[1] && part[2] <= bed[2];
    }
}
