namespace PrintGrid.Infrastructure.Shared.FileStorage;

public class MinioOptions
{
    public const string SectionName = "MinIO";

    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public string ModelBucket { get; set; } = "printgrid-models";
    public string SliceBucket { get; set; } = "printgrid-slices";
    public string PhotoBucket { get; set; } = "printgrid-photos";
}
