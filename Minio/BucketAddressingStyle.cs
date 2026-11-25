namespace Minio;

/// <summary>
/// Controls how bucket names are represented in request URLs.
/// </summary>
public enum BucketAddressingStyle
{
    /// <summary>
    /// Use the traditional path-style URL: <c>https://endpoint/bucket/key</c>.
    /// </summary>
    Path,

    /// <summary>
    /// Use a virtual-hosted-style URL: <c>https://bucket.endpoint/key</c>.
    /// </summary>
    VirtualHost,
}
