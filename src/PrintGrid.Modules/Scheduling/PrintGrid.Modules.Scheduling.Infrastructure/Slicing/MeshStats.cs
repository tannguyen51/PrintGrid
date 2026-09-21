namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

/// <summary>Parsed mesh statistics shared by the STL/OBJ parsers. Dimensions in mm, volume in cm³.</summary>
internal sealed record MeshStats(double WidthMm, double DepthMm, double HeightMm, double VolumeCm3, int VertexCount, int FaceCount);