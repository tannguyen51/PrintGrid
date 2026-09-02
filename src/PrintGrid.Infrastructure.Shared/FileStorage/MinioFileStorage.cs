using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace PrintGrid.Infrastructure.Shared.FileStorage;

public class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;

    public MinioFileStorage(IOptions<MinioOptions> options)
    {
        var config = options.Value;
        _client = new MinioClient()
            .WithEndpoint(config.Endpoint)
            .WithCredentials(config.AccessKey, config.SecretKey)
            .WithSSL(config.UseSsl)
            .Build();
    }

    public async Task<string> UploadAsync(
        string bucket,
        string objectName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(bucket, cancellationToken);

        var args = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType);

        await _client.PutObjectAsync(args, cancellationToken);
        return $"{bucket}/{objectName}";
    }

    public async Task<Stream> DownloadAsync(
        string bucket,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        var buffer = new MemoryStream();

        var args = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(buffer));

        await _client.GetObjectAsync(args, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    public Task<string> GetPresignedUrlAsync(string bucket, string objectName, TimeSpan expiry)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithExpiry((int)expiry.TotalSeconds);

        return _client.PresignedGetObjectAsync(args);
    }

    public Task DeleteAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName);

        return _client.RemoveObjectAsync(args, cancellationToken);
    }

    public async Task EnsureBucketAsync(string bucket, CancellationToken cancellationToken = default)
    {
        var exists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucket), cancellationToken);

        if (!exists)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cancellationToken);
        }
    }
}
