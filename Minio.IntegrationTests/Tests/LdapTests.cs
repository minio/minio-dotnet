using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Minio.CredentialProviders;
using Testcontainers.Keycloak;
using Testcontainers.Minio;
using Xunit;

namespace Minio.IntegrationTests.Tests;

public class LdapTests
{
    private class DefaultHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private class KeycloakAccessTokenProvider : IAccessTokenProvider
    {
        private readonly string _endpoint;
        private readonly string _realm;
        private readonly string _clientName;
        private readonly string _clientSecret;

        public KeycloakAccessTokenProvider(string endpoint, string realm, string clientName, string clientSecret)
        {
            _endpoint = endpoint;
            _realm = realm;
            _clientName = clientName;
            _clientSecret = clientSecret;
        }
    
        public async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            var tokenEndpoint = new Uri($"{_endpoint}/realms/{_realm}/protocol/openid-connect/token", UriKind.Absolute);

            using var http = new HttpClient();
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientName,
                ["client_secret"] = _clientSecret
            });
            var response = await http.PostAsync(tokenEndpoint, form, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            return json.RootElement.GetProperty("access_token").GetString()!;
        }
    }
    
    [Fact]
    public async Task TestWebIdentityLogin()
    {
        // Start Keycloak container
        await using var keycloakContainer = new KeycloakBuilder(Images.Keycloak)
            .WithHostname("keycloak")
            .WithExposedPort(KeycloakBuilder.KeycloakPort)
            .Build();
        await keycloakContainer.StartAsync();

        // Create realm
        const string realmName = "minio";
        using var keycloakClient = await GetKeycloakClientAsync(keycloakContainer.GetBaseAddress(), KeycloakBuilder.DefaultUsername, KeycloakBuilder.DefaultPassword);
        var realm = new Dictionary<string, object>
        {
            ["realm"] = realmName,
            ["enabled"] = true
        };
        using (var json = Json(realm))
        {
            var newRealmResponse = await keycloakClient.PostAsync(new Uri("/admin/realms", UriKind.Relative), json);
            Assert.True(newRealmResponse.IsSuccessStatusCode, "Failed to create realm");
        }
        
        // Create client
        var clientId = Guid.NewGuid().ToString();
        const string clientName = "minio-client";
        const string clientSecret = "minio-secret";
        var client = new Dictionary<string, object>
        {
            ["id"] = clientId,
            ["clientId"] = clientName,
            ["secret"] = clientSecret,
            ["directAccessGrantsEnabled"] = false,
            ["name"] = "Minio client",
            ["protocol"] = "openid-connect",
            ["publicClient"] = false, ["serviceAccountsEnabled"] = true,
            ["attributes"] = new Dictionary<string, object>
            {
                { "access.token.lifespan", 3600 }
            }
        };
        using (var json = Json(client))
        {
            var newClientResponse = await keycloakClient.PostAsync(new Uri($"/admin/realms/{realmName}/clients", UriKind.Relative), json);
            Assert.True(newClientResponse.IsSuccessStatusCode, "Failed to create client");
        }

        // Create client
        var protocolMapper = new Dictionary<string, object>
        {
            ["name"] = "test1",
            ["protocol"] = "openid-connect",
            ["protocolMapper"] = "oidc-hardcoded-claim-mapper",
            ["config"] = new Dictionary<string, object>
            {
                ["access.token.claim"] = "true",
                ["access.tokenResponse.claim"] = true,
                ["claim.name"] = "policy",
                ["claim.value"] = "consoleAdmin",
                ["jsonType.label"] = "String",
            }
        };
        using (var json = Json(protocolMapper))
        {
            var newClientResponse = await keycloakClient.PostAsync(new Uri($"/admin/realms/{realmName}/clients/{clientId}/protocol-mappers/models", UriKind.Relative), json);
            Assert.True(newClientResponse.IsSuccessStatusCode, "Failed to create client protocol mapper");
        }
        
        await using var minioContainer = new MinioBuilder(Images.AIStor)
            .WithEnvironment(new Dictionary<string, string>
            {
                ["MINIO_LICENSE"] = License.Minio,
                ["MINIO_IDENTITY_OPENID_CONFIG_URL"] = $"http://{keycloakContainer.IpAddress}:8080/realms/{realmName}/.well-known/openid-configuration",
                ["MINIO_IDENTITY_OPENID_CLIENT_ID"] = clientName,
                ["MINIO_IDENTITY_OPENID_CLIENT_SECRET"] = clientSecret,
                ["MINIO_IDENTITY_OPENID_CLAIM_NAME"] = "policy",
                ["MINIO_IDENTITY_OPENID_CLAIM_PREFIX"] = "",
                
            })
            .Build();
        await minioContainer.StartAsync();

        var httpClientFactory = new DefaultHttpClientFactory();
        var keycloakAccessTokenProvider = new KeycloakAccessTokenProvider(keycloakContainer.GetBaseAddress(), realmName, clientName, clientSecret);
        var webIdentityOptions = new WebIdentityCredentialsOptions { StsEndPoint = minioContainer.GetConnectionString() };
        var identityProvider = new WebIdentityProvider(httpClientFactory, keycloakAccessTokenProvider, Options.Create(webIdentityOptions));
        var minioClient = new MinioClientBuilder(minioContainer.GetConnectionString())
            .WithCredentialsProvider(identityProvider)
            .Build();

        await minioClient.ListBucketsAsync().CountAsync();
    }

    public static async Task<HttpClient> GetKeycloakClientAsync(string endpoint, string username, string password, string realm = "master", CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        var tokenEndpoint = new Uri($"{endpoint}/realms/{realm}/protocol/openid-connect/token", UriKind.Absolute);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = "admin-cli", 
            ["grant_type"] = "password",
            ["username"] = username, 
            ["password"] = password, 
        });
        var response = await client.PostAsync(tokenEndpoint, form, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        var accessToken = json.RootElement.GetProperty("access_token").GetString()!;
        return new HttpClient
        {
            BaseAddress = new Uri(endpoint),
            DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
        };
    }

    private static StringContent Json(object obj) => new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
}
