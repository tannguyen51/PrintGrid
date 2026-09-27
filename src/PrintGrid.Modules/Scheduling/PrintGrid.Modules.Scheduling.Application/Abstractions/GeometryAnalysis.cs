namespace PrintGrid.Modules.Scheduling.Application.Abstractions;

/// <summary>
/// Result of parsing/stats of a model file (FR-SCHED-001). Dimensions and volume in mm / cm³.
/// </summary>
public record GeometryAnalysis(
    bool IsValid,
    decimal WidthMm,
    decimal DepthMm,
    decimal HeightMm,
    decimal VolumeCm3,
    int VertexCount,
    int FaceCount,
    string? ErrorMessage = null)
{
    public static GeometryAnalysis Failed(string reason) =>
        new(false, 0, 0, 0, 0, 0, 0, reason);
}