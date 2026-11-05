namespace PrintGrid.Infrastructure.Shared.FileStorage;

public class MinioOptions
{
    public const string SectionName = "MinIO";

    public string Endpoint { get; init; } = "localhost:9000";
    public string? PublicEndpoint { get; init; }
    /// <summary>
    /// SigV4 region. Self-hosted MinIO uses "us-east-1"; Cloudflare R2 uses "auto";
    /// Backblaze B2 uses its cluster id (e.g. "us-west-004").
    /// </summary>
    public string Region { get; init; } = "us-east-1";
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public bool UseSsl { get; init; }
    public string ModelBucket { get; init; } = "printgrid-models";
    public string SliceBucket { get; init; } = "printgrid-slices";
    public string PhotoBucket { get; init; } = "printgrid-photos";
}
