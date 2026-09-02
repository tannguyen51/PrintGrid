using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.ValueObjects;

public sealed class BuildVolume : ValueObject
{
    public decimal WidthMm { get; }
    public decimal DepthMm { get; }
    public decimal HeightMm { get; }

    private BuildVolume() { }

    private BuildVolume(decimal widthMm, decimal depthMm, decimal heightMm)
    {
        WidthMm = widthMm;
        DepthMm = depthMm;
        HeightMm = heightMm;
    }

    public static BuildVolume Create(decimal widthMm, decimal depthMm, decimal heightMm)
    {
        if (widthMm <= 0 || depthMm <= 0 || heightMm <= 0)
            throw new ArgumentException("Build volume dimensions must be positive");

        return new BuildVolume(widthMm, depthMm, heightMm);
    }

    public bool CanFit(BuildVolume part) =>
        part.WidthMm <= WidthMm && part.DepthMm <= DepthMm && part.HeightMm <= HeightMm;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return WidthMm;
        yield return DepthMm;
        yield return HeightMm;
    }

    public override string ToString() => $"{WidthMm}x{DepthMm}x{HeightMm} mm";
}
