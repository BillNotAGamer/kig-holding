using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using KIGHolding.Options;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services;

public sealed class CloudflareR2Client : ICloudflareR2Client, IDisposable
{
    private const string ImmutableCacheControl = "public,max-age=31536000,immutable";

    private readonly ImageStorageSettings _settings;
    private AmazonS3Client? _client;

    public CloudflareR2Client(IOptions<ImageStorageSettings> options)
    {
        _settings = options.Value;
    }

    public async Task PutObjectAsync(
        string bucketName,
        string objectKey,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            DisableDefaultChecksumValidation = true,
            DisablePayloadSigning = true,
            UseChunkEncoding = false
        };
        request.Headers.CacheControl = ImmutableCacheControl;

        await GetClient().PutObjectAsync(request, cancellationToken);
    }

    public async Task DeleteObjectAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        await GetClient().DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        }, cancellationToken);
    }

    public async Task<bool> ObjectExistsAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await GetClient().GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = objectKey
            }, cancellationToken);

            return true;
        }
        catch (AmazonS3Exception exception) when (IsNotFound(exception))
        {
            return false;
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private AmazonS3Client GetClient()
    {
        if (_client is not null)
        {
            return _client;
        }

        var credentials = new BasicAWSCredentials(_settings.AccessKeyId, _settings.SecretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl.Trim(),
            ForcePathStyle = _settings.UsePathStyle,
            AuthenticationRegion = string.IsNullOrWhiteSpace(_settings.Region) ? "auto" : _settings.Region.Trim()
        };

        _client = new AmazonS3Client(credentials, config);
        return _client;
    }

    private static bool IsNotFound(AmazonS3Exception exception)
    {
        return exception.StatusCode == HttpStatusCode.NotFound ||
               string.Equals(exception.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase);
    }
}
