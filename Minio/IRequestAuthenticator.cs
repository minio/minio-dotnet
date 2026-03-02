using Minio.Model;

namespace Minio;

internal interface IRequestAuthenticator
{
    ValueTask AuthenticateAsync(HttpRequestMessage request, string region, string service, CancellationToken cancellationToken);

    ValueTask<Uri> PresignAsync(HttpRequestMessage request, string region, string service, TimeSpan expiry, CancellationToken cancellationToken);

    ValueTask<PostPolicyResult> PresignPostPolicyAsync(Uri bucketUri, string bucketName, string key, TimeSpan expiry, string region, string service, IEnumerable<PostPolicyCondition>? conditions, CancellationToken cancellationToken);
}