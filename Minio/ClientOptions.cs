namespace Minio;

/// <summary>
/// Configuration options for the MinIO client, including endpoint, region, and HTTP client settings.
/// </summary>
public class ClientOptions
{
    /// <summary>
    /// Gets or sets the URI of the MinIO or S3-compatible endpoint to connect to.
    /// </summary>
    public required Uri EndPoint { get; set; }

    /// <summary>
    /// Gets or sets the AWS region used for request signing. Defaults to <c>us-east-1</c>.
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Gets or sets the named <see cref="System.Net.Http.HttpClient"/> to use for outbound HTTP requests.
    /// Defaults to <c>Minio</c>.
    /// </summary>
    public string MinioHttpClient { get; set; } = "Minio";
}
