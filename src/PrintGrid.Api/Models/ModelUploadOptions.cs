namespace PrintGrid.Api.Models;

public sealed class ModelUploadOptions
{
    public const string SectionName = "ModelUpload";
    public long MaxFileBytes { get; init; } = 50L * 1024 * 1024;
    public int MaxModelsPerCustomer { get; init; } = 20;
    public long MaxStorageBytesPerCustomer { get; init; } = 1024L * 1024 * 1024;
}
