using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

/// <summary>
/// Built-in slicing pipeline: parses STL/OBJ for true geometry and estimates print time
/// with a heuristic toolpath model (fixed extrusion width, travel/layer overhead, warmup).
/// Until binary model upload (MinIO) is wired, <see cref="EstimateFromMetadata"/> derives a
/// deterministic placeholder from the file size — it is a documented mock, not a slicer.
/// </summary>
public class PrintSlicingService : ISlicingService
{
    // Per-technology toolpath assumptions — sanity defaults, calibration target later (FR-SCHED-008).
    private const decimal FdmExtrusionWidthMm = 0.4m;
    private const decimal SlaExtrusionWidthMm = 0.25m;
    private const decimal FdmFeedMmPerMin = 3200m;  // ~53 mm/s
    private const decimal SlaFeedMmPerMin = 2600m;
    private const decimal SlsFeedMmPerMin = 2200m;
    private const decimal LayerOverheadMinutesPerLayer = 0.06m; // ~3.6s hop/recoating
    private const decimal WarmUpMinutes = 10m;
    private const int MinimumPrintMinutes = 5;

    private const double BytesPerSolidMm3 = 1.0 / 3.0; // metadata fallback only

    private static readonly Dictionary<string, decimal> MaterialDensityGramsPerCm3 = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PLA"] = 1.24m,
        ["PETG"] = 1.27m,
        ["ABS"] = 1.04m,
        ["NYLON"] = 1.14m,
        ["TPU"] = 1.21m,
        ["RESIN"] = 1.10m,
        ["ASA"] = 1.07m,
        ["PC"] = 1.20m
    };

    public Task<GeometryAnalysis> AnalyzeAsync(Stream fileStream, string fileFormat, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var format = (fileFormat ?? string.Empty).Trim().ToUpperInvariant();
            var mesh = format switch
            {
                "STL" or ".STL" => StlMeshParser.Parse(fileStream),
                "OBJ" or ".OBJ" => ObjMeshParser.Parse(fileStream),
                "3MF" or ".3MF" => throw new NotSupportedException("3MF geometry parsing is not implemented yet"),
                _ => throw new NotSupportedException($"Unsupported model format '{fileFormat}'")
            };

            var analysis = new GeometryAnalysis(
                IsValid: true,
                WidthMm: decimal.Round((decimal)mesh.WidthMm, 2),
                DepthMm: decimal.Round((decimal)mesh.DepthMm, 2),
                HeightMm: decimal.Round((decimal)mesh.HeightMm, 2),
                VolumeCm3: decimal.Round((decimal)mesh.VolumeCm3, 3),
                mesh.VertexCount,
                mesh.FaceCount);

            return Task.FromResult(analysis);
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or EndOfStreamException)
        {
            return Task.FromResult(GeometryAnalysis.Failed(ex.Message));
        }
    }

    public PrintEstimate Estimate(
        GeometryAnalysis geometry,
        string materialCode,
        decimal layerHeightMm,
        int infillPercent,
        PrintTechnology technology)
    {
        if (!geometry.IsValid)
            throw new ArgumentException("Cannot estimate over invalid geometry", nameof(geometry));

        var minutes = ComputeMinutes(geometry.VolumeCm3, geometry.HeightMm, materialCode, layerHeightMm, infillPercent, technology);
        var grams = ComputeGrams(geometry.VolumeCm3, materialCode, infillPercent);
        return new PrintEstimate(minutes, decimal.Round(grams, 2));
    }

    public GeometryAnalysis AnalyzeFromMetadata(long sizeBytes)
    {
        // Documented mock: treat the file as an axis-aligned box whose solid volume scales
        // with its byte count. Replaced by real geometry once binary upload is available.
        var solidMm3 = Math.Max(1024, sizeBytes * BytesPerSolidMm3);
        var side = Math.Cbrt(solidMm3);
        var fakeHeight = side * 1.25;

        return new GeometryAnalysis(
            IsValid: true,
            WidthMm: (decimal)Math.Round(side, 2),
            DepthMm: (decimal)Math.Round(side, 2),
            HeightMm: (decimal)Math.Round(fakeHeight, 2),
            VolumeCm3: (decimal)Math.Round(solidMm3 / 1000.0, 3),
            VertexCount: 0,
            FaceCount: 0);
    }

    public PrintEstimate EstimateFromMetadata(
        long sizeBytes,
        string materialCode,
        decimal layerHeightMm,
        int infillPercent,
        PrintTechnology technology) =>
        Estimate(AnalyzeFromMetadata(sizeBytes), materialCode, layerHeightMm, infillPercent, technology);

    private static int ComputeMinutes(
        decimal volumeCm3,
        decimal heightMm,
        string materialCode,
        decimal layerHeightMm,
        int infillPercent,
        PrintTechnology technology)
    {
        if (layerHeightMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(layerHeightMm), "Layer height must be positive");

        var extrusionWidth = technology switch
        {
            PrintTechnology.Fdm => FdmExtrusionWidthMm,
            PrintTechnology.Sla => SlaExtrusionWidthMm,
            _ => FdmExtrusionWidthMm
        };
        var feed = technology switch
        {
            PrintTechnology.Fdm => FdmFeedMmPerMin,
            PrintTechnology.Sla => SlaFeedMmPerMin,
            _ => SlsFeedMmPerMin,
        };

        // Walls/perimeters always print solid; infill only inside. 10% accounts for supports + brim.
        var fillFraction = Math.Max(0.15m, Math.Clamp(infillPercent, 0, 100) / 100m) * 1.10m;
        var materialVolumeCm3 = volumeCm3 * fillFraction;

        var toolpathMm = materialVolumeCm3 * 1000m / (extrusionWidth * layerHeightMm);
        var layers = Math.Max(1m, heightMm / layerHeightMm);
        var printMinutes = toolpathMm / feed + layers * LayerOverheadMinutesPerLayer + WarmUpMinutes;

        return Math.Max(MinimumPrintMinutes, (int)Math.Ceiling(decimal.ToDouble(printMinutes)));
    }

    private static decimal ComputeGrams(decimal volumeCm3, string materialCode, int infillPercent)
    {
        var density = MaterialDensityGramsPerCm3.GetValueOrDefault(materialCode, 1.15m);
        var fillFraction = Math.Max(0.15m, Math.Clamp(infillPercent, 0, 100) / 100m) * 1.10m;
        return volumeCm3 * fillFraction * density;
    }
}