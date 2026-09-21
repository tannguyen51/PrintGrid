using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Modules.Scheduling.Application.Abstractions;

/// <summary>
/// Geometry analysis and print-time/material estimation.
/// A real slicing engine (CuraEngine/PrusaSlicer CLI) can be swapped in behind this
/// interface; the built-in implementation parses STL/OBJ for geometry and uses a
/// heuristic toolpath model for estimation, with a documented metadata fallback
/// for models whose binary file is not (yet) stored (see <see cref="EstimateFromMetadata"/>).
/// </summary>
public interface ISlicingService
{
    /// <summary>Parses a model stream (STL/OBJ) and computes bounding box, volume and mesh stats.</summary>
    Task<GeometryAnalysis> AnalyzeAsync(Stream fileStream, string fileFormat, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deterministic placeholder geometry derived purely from file size — used while binary
    /// model upload (MinIO) is not yet wired.
    /// </summary>
    GeometryAnalysis AnalyzeFromMetadata(long sizeBytes);

    /// <summary>Estimates print time and material usage for a configuration over known geometry.</summary>
    PrintEstimate Estimate(GeometryAnalysis geometry, string materialCode, decimal layerHeightMm, int infillPercent, PrintTechnology technology);

    /// <summary>
    /// Deterministic fallback estimate derived purely from file size — used while binary
    /// model upload (MinIO) is not yet wired. Produces a plausible part envelope whose
    /// volume scales with the file's byte size.
    /// </summary>
    PrintEstimate EstimateFromMetadata(long sizeBytes, string materialCode, decimal layerHeightMm, int infillPercent, PrintTechnology technology);
}