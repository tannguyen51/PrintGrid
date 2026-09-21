using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Enums;
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
    private readonly ILogger<AnalyzeModelJob> _logger;

    public AnalyzeModelJob(
        IModelRepository models,
        ISlicingService slicing,
        IUnitOfWork unitOfWork,
        ILogger<AnalyzeModelJob> logger)
    {
        _models = models;
        _slicing = slicing;
        _unitOfWork = unitOfWork;
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
            var geometry = _slicing.AnalyzeFromMetadata(model.SizeBytes);
            var estimate = _slicing.Estimate(geometry, "PLA", 0.2m, 30, PrintTechnology.Fdm);

            model.ApplyGeometry(
                geometry.WidthMm,
                geometry.DepthMm,
                geometry.HeightMm,
                geometry.VolumeCm3,
                estimate.PrintMinutes);

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
}