namespace PrintGrid.Infrastructure.Shared.FileStorage;

public interface IFileStorage
{
    Task<string> UploadAsync(
        string bucket,
        string objectName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string bucket, string objectName, CancellationToken cancellationToken = default);

    Task<string> GetPresignedUrlAsync(string bucket, string objectName, TimeSpan expiry);

    Task DeleteAsync(string bucket, string objectName, CancellationToken cancellationToken = default);

    Task EnsureBucketAsync(string bucket, CancellationToken cancellationToken = default);
}
