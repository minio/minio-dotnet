using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Minio.CredentialProviders;

/// <summary>
/// Resolves the AWS web-identity (IRSA) configuration from the process environment.
/// Used by <see cref="WebIdentityProvider.FromAwsEnvironment"/>.
/// </summary>
internal static class AwsWebIdentityEnvironment
{
    internal const string DefaultStsEndpoint = "https://sts.amazonaws.com";

    /// <summary>
    /// Resolves the STS endpoint. Precedence: <paramref name="explicitEndpoint"/>, <c>AWS_ENDPOINT_URL_STS</c>,
    /// then <c>sts[.region].amazonaws.com[.cn]</c>. The global <c>AWS_ENDPOINT_URL</c> is deliberately not read,
    /// so only setting the STS-specific variable changes behavior. <c>AWS_ENDPOINT_URL_STS</c> is skipped when
    /// <c>AWS_IGNORE_CONFIGURED_ENDPOINT_URLS</c> is <c>true</c> (case-insensitive).
    /// See https://docs.aws.amazon.com/sdkref/latest/guide/feature-ss-endpoints.html
    /// </summary>
    internal static string ResolveStsEndpoint(string? explicitEndpoint) => ResolveStsEndpoint(explicitEndpoint, out _);

    /// <param name="explicitEndpoint">Endpoint given in code, if any.</param>
    /// <param name="fromStsVariable"><see langword="true"/> when the result came from <c>AWS_ENDPOINT_URL_STS</c>.</param>
    internal static string ResolveStsEndpoint(string? explicitEndpoint, out bool fromStsVariable)
    {
        fromStsVariable = false;
        if (!string.IsNullOrEmpty(explicitEndpoint)) return explicitEndpoint;

        if (!string.Equals(GetEnv("AWS_IGNORE_CONFIGURED_ENDPOINT_URLS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            var fromEnv = GetEnv("AWS_ENDPOINT_URL_STS");
            if (fromEnv != null)
            {
                fromStsVariable = true;
                return fromEnv;
            }
        }

        var region = GetEnv("AWS_REGION");
        if (region == null) return DefaultStsEndpoint;
        var suffix = region.StartsWith("cn-", StringComparison.Ordinal) ? ".amazonaws.com.cn" : ".amazonaws.com";
        return "https://sts." + region + suffix;
    }

    /// <summary>
    /// Loads the PEM certificates in <paramref name="path"/>. Throws when the file cannot be read or has no certificate.
    /// </summary>
    internal static X509Certificate2Collection LoadCaBundle(string path)
    {
        string pem;
        try
        {
            pem = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new InvalidOperationException("Unable to read AWS_CA_BUNDLE: " + ex.Message, ex);
        }

        var certs = new X509Certificate2Collection();
        try
        {
            certs.ImportFromPem(pem);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            throw new InvalidOperationException($"AWS_CA_BUNDLE \"{path}\" contains an invalid PEM certificate.", ex);
        }

        if (certs.Count == 0)
            throw new InvalidOperationException($"AWS_CA_BUNDLE \"{path}\" contains no PEM certificates.");
        return certs;
    }

    /// <summary>
    /// Server certificate validation that accepts what the platform trusts, plus chains that end in one of
    /// <paramref name="bundle"/>. Host name mismatches are never accepted.
    /// </summary>
    internal static bool ValidateWithBundle(X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors, X509Certificate2Collection bundle)
    {
        if (errors == SslPolicyErrors.None) return true;
        if (certificate == null || errors != SslPolicyErrors.RemoteCertificateChainErrors) return false;

        using var custom = new X509Chain();
        custom.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        custom.ChainPolicy.CustomTrustStore.AddRange(bundle);
        custom.ChainPolicy.ApplicationPolicy.Add(new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.1")); // serverAuth, as SslStream requires
        custom.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        if (chain != null)
        {
            foreach (var element in chain.ChainElements)
                custom.ChainPolicy.ExtraStore.Add(element.Certificate);
        }
        return custom.Build(certificate);
    }

    /// <summary>
    /// Creates a factory. Without a caller client, and only when <paramref name="useCaBundle"/> is set (the endpoint
    /// came from <c>AWS_ENDPOINT_URL_STS</c>), every <c>CreateClient</c> call (that is, every credential retrieval)
    /// reads <c>AWS_CA_BUNDLE</c> and trusts it in addition to the platform trust store. Otherwise the variable is
    /// not read at all.
    /// </summary>
    internal static IHttpClientFactory CreateFactory(HttpClient? callerClient, bool useCaBundle) =>
        callerClient != null ? new BorrowedClientFactory(callerClient) : new BundleClientFactory(useCaBundle);

    private sealed class BundleClientFactory : IHttpClientFactory
    {
        private readonly bool _useCaBundle;
        public BundleClientFactory(bool useCaBundle) => _useCaBundle = useCaBundle;

        public HttpClient CreateClient(string name)
        {
            var handler = new SocketsHttpHandler();
            var caBundle = _useCaBundle ? GetEnv("AWS_CA_BUNDLE") : null;
            if (caBundle != null)
            {
                try
                {
                    var certs = LoadCaBundle(caBundle);
                    handler.SslOptions.RemoteCertificateValidationCallback = (_, cert, chain, errors) =>
                        ValidateWithBundle(cert as X509Certificate2, chain, errors, certs);
                }
                catch
                {
                    handler.Dispose();
                    throw;
                }
            }
            return new HttpClient(handler, disposeHandler: true);
        }
    }

    internal static string? GetEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    // Forwards to the caller's client without ever disposing or reconfiguring it.
    private sealed class BorrowedClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public BorrowedClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) =>
            // The caller's client and cancellation token decide the timeout, not this wrapper's 100 s default.
            new(new Forwarder(_client), disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };

        private sealed class Forwarder : HttpMessageHandler
        {
            private readonly HttpClient _client;
            public Forwarder(HttpClient client) => _client = client;
            // HttpClient refuses to send a message twice, so forward a copy.
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                using var copy = new HttpRequestMessage(request.Method, request.RequestUri)
                {
                    Content = request.Content,
                    Version = request.Version,
                };
                foreach (var header in request.Headers) copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
                return await _client.SendAsync(copy, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
