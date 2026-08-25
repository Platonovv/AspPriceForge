using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PriceForge.Application.Images;

namespace PriceForge.Infrastructure.Images;

public sealed class ImageStorage : IImageStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageSettings _settings;
    private readonly AmazonS3Client _signingClient;

    public ImageStorage(IAmazonS3 s3Client, IOptions<StorageSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(s3Client);
        ArgumentNullException.ThrowIfNull(settings);

        _s3Client = s3Client;
        _settings = settings.Value;

        _signingClient = new AmazonS3Client(new BasicAWSCredentials(_settings.AccessKey, _settings.SecretKey),
                                            new AmazonS3Config
                                            {
                                                ServiceURL = _settings.PublicEndpoint, ForcePathStyle = true
                                            });
    }

    public async Task SaveAsync(string key, Stream inputStream, string contentType, CancellationToken cancellationToken)
        => await _s3Client.PutObjectAsync(new PutObjectRequest
                                          {
                                              BucketName = _settings.Bucket,
                                              Key = key,
                                              InputStream = inputStream,
                                              ContentType = contentType
                                          },
                                          cancellationToken);

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
        => await _s3Client.DeleteObjectAsync(new DeleteObjectRequest {BucketName = _settings.Bucket, Key = key},
                                             cancellationToken);

    public string GetDownloadUrl(string key)
        => _signingClient.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _settings.Bucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(
                TimeSpan.FromMinutes(_settings.DownloadUrlMinutes)),
            Verb = HttpVerb.GET,
            Protocol = _settings.PublicEndpoint.StartsWith(
                           "https",
                           StringComparison.OrdinalIgnoreCase)
                           ? Protocol.HTTPS
                           : Protocol.HTTP
        });

    public async Task<List<string>> ListKeysAsync(DateTime threshold, CancellationToken stoppingToken)
    {
        var keys = new List<string>();
        string? continuationToken = null;
        bool isTruncated;

        do
        {
            ListObjectsV2Response response = await _s3Client.ListObjectsV2Async(
                                                 new ListObjectsV2Request
                                                 {
                                                     BucketName = _settings.Bucket,
                                                     ContinuationToken = continuationToken
                                                 },
                                                 stoppingToken);

            if (response.S3Objects is not null)
            {
                keys.AddRange(response.S3Objects.Where(o => o.LastModified < threshold).Select(o => o.Key));
            }

            continuationToken = response.NextContinuationToken;
            isTruncated = response.IsTruncated == true;
        }
        while (isTruncated);

        return keys;
    }

    public async Task<(int deleteCount, int deleteErrorCount)> DeleteExpiredKeysAsync(
        List<string> keys,
        CancellationToken stoppingToken)
    {
        var allKeyVersions = keys.Select(key => new KeyVersion {Key = key}).ToList();

        if (allKeyVersions.Count == 0)
        {
            return (0, 0);
        }

        var keyVersions = allKeyVersions.Chunk(1000).ToList();

        int deleteCount = 0;
        int deleteErrorCount = 0;

        foreach (KeyVersion[] keyVersion in keyVersions)
        {
            DeleteObjectsResponse delete = await _s3Client.DeleteObjectsAsync(
                                               new DeleteObjectsRequest
                                               {
                                                   BucketName = _settings.Bucket, Objects = keyVersion.ToList()
                                               },
                                               stoppingToken);

            deleteCount += delete.DeletedObjects?.Count ?? 0;
            deleteErrorCount += delete.DeleteErrors?.Count ?? 0;
        }

        return (deleteCount, deleteErrorCount);
    }
}
