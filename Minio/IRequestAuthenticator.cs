namespace Minio;

internal interface IRequestAuthenticator
{
    ValueTask AuthenticateAsync(HttpRequestMessage request, string region, string service, CancellationToken cancellationToken);
}