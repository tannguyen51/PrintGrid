using PrintGrid.Modules.Scheduling.Application.Abstractions;

namespace PrintGrid.Api.Models;

public static class ModelFileInspector
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".stl", ".obj", ".3mf", ".glb"
    };

    public static bool IsAllowedExtension(string extension) => AllowedExtensions.Contains(extension);

    public static async Task<string?> ValidateContentAsync(
        Stream content,
        string extension,
        ISlicingService slicingService,
        CancellationToken cancellationToken)
    {
        content.Position = 0;
        var analysis = await slicingService.AnalyzeAsync(content, extension, cancellationToken);
        content.Position = 0;
        return analysis.IsValid ? null : $"Nội dung file không phải {extension.TrimStart('.').ToUpperInvariant()} hợp lệ.";
    }
}
