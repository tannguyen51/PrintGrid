namespace PrintGrid.Infrastructure.Shared.FileStorage;

public class MinioOptions
{
    public const string SectionName = "MinIO";

    public string Endpoint { get; set; } = "localhost:9000";
    public string? PublicEndpoint { get; set; }
    /// <summary>
    /// SigV4 region. Self-hosted MinIO uses "us-east-1"; Cloudflare R2 uses "auto";
    /// Backblaze B2 uses its cluster id (e.g. "us-west-004").
    /// </summary>
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public string ModelBucket { get; set; } = "printgrid-models";
    public string SliceBucket { get; set; } = "printgrid-slices";
    public string PhotoBucket { get; set; } = "printgrid-photos";
}
