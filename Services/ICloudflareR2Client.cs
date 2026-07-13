namespace KIGHolding.Services;

public interface ICloudflareR2Client
{
    Task PutObjectAsync(
        string bucketName,
        string objectKey,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);

    Task DeleteObjectAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default);

    Task<bool> ObjectExistsAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default);
}
