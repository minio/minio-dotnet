namespace Minio.CredentialProviders;

/// <summary>
/// Configuration options for <see cref="KeycloakAccessTokenProvider"/>.
/// Specifies the Keycloak server connection details used to obtain a client credentials token.
/// </summary>
public class KeycloakAccessTokenProviderOptions
{
    /// <summary>
    /// Gets or sets the base URL of the Keycloak server (e.g., <c>https://keycloak.example.com</c>).
    /// This value is required.
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the name of the Keycloak realm in which the client is registered.
    /// This value is required.
    /// </summary>
    public required string Realm { get; set; }

    /// <summary>
    /// Gets or sets the client ID (name) registered in the Keycloak realm.
    /// This value is required.
    /// </summary>
    public required string ClientName { get; set; }

    /// <summary>
    /// Gets or sets the client secret associated with the registered client.
    /// This value is required.
    /// </summary>
    public required string ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the named <see cref="System.Net.Http.HttpClient"/> to create via
    /// <see cref="System.Net.Http.IHttpClientFactory"/> when calling the token endpoint.
    /// Defaults to <c>"Keyclock"</c>.
    /// </summary>
    public string HttpClientName { get; set; } = "Keyclock";
}
