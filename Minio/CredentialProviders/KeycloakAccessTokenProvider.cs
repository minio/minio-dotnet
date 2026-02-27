using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Minio.CredentialProviders;

/// <summary>
/// An <see cref="IAccessTokenProvider"/> that obtains a short-lived access token from a
/// Keycloak server using the OAuth 2.0 client credentials grant
/// (<c>grant_type=client_credentials</c>). The token is fetched on every call;
/// caching is the responsibility of the caller.
/// </summary>
public sealed class KeycloakAccessTokenProvider : IAccessTokenProvider
{
    private readonly IOptions<KeycloakAccessTokenProviderOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="KeycloakAccessTokenProvider"/> with the supplied options
    /// and HTTP client factory.
    /// </summary>
    /// <param name="options">The <see cref="IOptions{TOptions}"/> wrapper containing the Keycloak connection settings.</param>
    /// <param name="httpClientFactory">The factory used to create the <see cref="System.Net.Http.HttpClient"/> for token requests.</param>
    public KeycloakAccessTokenProvider(IOptions<KeycloakAccessTokenProviderOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Asynchronously requests a new access token from the Keycloak token endpoint using
    /// the client credentials grant and returns the <c>access_token</c> string from the response.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{T}"/> that resolves to the access token string returned by Keycloak.
    /// </returns>
    /// <exception cref="System.Net.Http.HttpRequestException">
    /// Thrown when the token endpoint returns a non-success HTTP status code.
    /// </exception>
    public async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var opts = _options.Value;
            
        var tokenEndpoint = new Uri($"{opts.Endpoint}/realms/{opts.Realm}/protocol/openid-connect/token", UriKind.Absolute);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = opts.ClientName,
            ["client_secret"] = opts.ClientSecret
        });
        using var httpClient = _httpClientFactory.CreateClient(opts.HttpClientName);
        var response = await httpClient.PostAsync(tokenEndpoint, form, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        return json.RootElement.GetProperty("access_token").GetString()!;
    }
}