using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Minio.CredentialProviders;
using Minio.UnitTests.Services;
using Xunit;

namespace Minio.UnitTests.Tests;

// These tests change process-wide environment variables, so they must not run in parallel with each other.
[Collection("AwsEnvironment")]
public sealed class WebIdentityEnvironmentTests : IDisposable
{
    private static readonly string[] Variables =
    [
        "AWS_WEB_IDENTITY_TOKEN_FILE", "AWS_ROLE_ARN", "AWS_REGION", "AWS_ENDPOINT_URL",
        "AWS_ENDPOINT_URL_STS", "AWS_ROLE_SESSION_NAME", "AWS_IGNORE_CONFIGURED_ENDPOINT_URLS", "AWS_CA_BUNDLE",
    ];

    private readonly List<string> _files = [];
    private readonly Dictionary<string, string?> _saved = [];
    private readonly IWebProxy _savedProxy = HttpClient.DefaultProxy;
    private readonly string _tempDir = Directory.CreateTempSubdirectory("webidentity-tests").FullName;

    public WebIdentityEnvironmentTests()
    {
        foreach (var v in Variables)
        {
            _saved[v] = Environment.GetEnvironmentVariable(v);
            Environment.SetEnvironmentVariable(v, null);
        }
        // The TLS tests talk to a loopback server: never route that through a proxy from the test host's environment.
        HttpClient.DefaultProxy = new NoProxy();
    }

    public void Dispose()
    {
        foreach (var (name, value) in _saved) Environment.SetEnvironmentVariable(name, value);
        HttpClient.DefaultProxy = _savedProxy;
        foreach (var f in _files) File.Delete(f);
        Directory.Delete(_tempDir, recursive: true);
    }

    // A path inside a directory this test owns; the file is never created.
    private string MissingFile() => Path.Combine(_tempDir, "missing.pem");

    private sealed class NoProxy : IWebProxy
    {
        public ICredentials? Credentials { get; set; }
        public Uri? GetProxy(Uri destination) => null;
        public bool IsBypassed(Uri host) => true;
    }

    private string TempFile(string content)
    {
        var path = Path.GetTempFileName();
        _files.Add(path);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void DefaultEndpointWithoutRegion()
    {
        Assert.Equal("https://sts.amazonaws.com", AwsWebIdentityEnvironment.ResolveStsEndpoint(null));
    }

    [Theory]
    [InlineData("eu-west-1", "https://sts.eu-west-1.amazonaws.com")]
    [InlineData("cn-north-1", "https://sts.cn-north-1.amazonaws.com.cn")]
    public void DefaultEndpointWithRegion(string region, string expected)
    {
        Environment.SetEnvironmentVariable("AWS_REGION", region);
        Assert.Equal(expected, AwsWebIdentityEnvironment.ResolveStsEndpoint(null));
    }

    // T4
    [Fact]
    public void ServiceEndpointBeatsRegionDefault()
    {
        Environment.SetEnvironmentVariable("AWS_REGION", "eu-west-1");
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com");
        Assert.Equal("https://sts.example.com", AwsWebIdentityEnvironment.ResolveStsEndpoint(null));
    }

    // T2: the global AWS_ENDPOINT_URL is not read, so the AWS default is used.
    [Fact]
    public void GlobalEndpointVariableIsIgnored()
    {
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL", "https://global.example.com");
        Assert.Equal("https://sts.amazonaws.com", AwsWebIdentityEnvironment.ResolveStsEndpoint(null));
        Environment.SetEnvironmentVariable("AWS_REGION", "eu-west-1");
        Assert.Equal("https://sts.eu-west-1.amazonaws.com", AwsWebIdentityEnvironment.ResolveStsEndpoint(null));
    }

    // T2, end to end: the request goes to the default host (recorded by a stub handler, no network).
    [Fact]
    public async Task GlobalEndpointVariableDoesNotRedirectTheRequest()
    {
        Environment.SetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE", TempFile("jwt"));
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL", "https://global.example.com");
        var (caller, seen) = RecordingClient();
        using (caller)
            await WebIdentityProvider.FromAwsEnvironment(httpClient: caller).GetCredentialsAsync(CancellationToken.None);
        Assert.Equal("sts.amazonaws.com", seen().Host);
    }

    // T3
    [Fact]
    public void CaBundleIsNotReadForExplicitOrDefaultEndpoint()
    {
        Environment.SetEnvironmentVariable("AWS_CA_BUNDLE", MissingFile());
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com");

        AwsWebIdentityEnvironment.ResolveStsEndpoint("https://explicit.example.com", out var explicitFromVar);
        Assert.False(explicitFromVar);
        using (AwsWebIdentityEnvironment.CreateFactory(null, explicitFromVar).CreateClient("Minio")) { }

        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", null);
        AwsWebIdentityEnvironment.ResolveStsEndpoint(null, out var defaultFromVar);
        Assert.False(defaultFromVar);
        using (AwsWebIdentityEnvironment.CreateFactory(null, defaultFromVar).CreateClient("Minio")) { }

        // ...but it is read, and its error surfaced, once the endpoint comes from AWS_ENDPOINT_URL_STS.
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com");
        AwsWebIdentityEnvironment.ResolveStsEndpoint(null, out var stsFromVar);
        Assert.True(stsFromVar);
        Assert.Throws<InvalidOperationException>(() => AwsWebIdentityEnvironment.CreateFactory(null, stsFromVar).CreateClient("Minio"));
    }

    // T3, end to end with a real handshake: an explicit endpoint ignores even a valid bundle.
    [Fact]
    public async Task ExplicitEndpointIgnoresCaBundle()
    {
        using var server = TlsServer.Start();
        Environment.SetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE", TempFile("jwt"));
        Environment.SetEnvironmentVariable("AWS_CA_BUNDLE", TempFile(server.CertificatePem));
        await AssertCertificateFailureAsync(WebIdentityProvider.FromAwsEnvironment(server.Url));
    }

    [Fact]
    public void ServiceEndpointWinsWhenGlobalVariableIsAlsoSet()
    {
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL", "https://global.example.com");
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com");
        Assert.Equal("https://sts.example.com", AwsWebIdentityEnvironment.ResolveStsEndpoint(null));
    }

    [Fact]
    public void ExplicitEndpointBeatsEnvironment()
    {
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com");
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL", "https://global.example.com");
        Assert.Equal("https://explicit.example.com", AwsWebIdentityEnvironment.ResolveStsEndpoint("https://explicit.example.com"));
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("True")]
    public void IgnoreFlagSkipsStsVariable(string flag)
    {
        Environment.SetEnvironmentVariable("AWS_IGNORE_CONFIGURED_ENDPOINT_URLS", flag);
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com");
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL", "https://global.example.com");
        Assert.Equal("https://sts.amazonaws.com", AwsWebIdentityEnvironment.ResolveStsEndpoint(null));
    }

    [Fact]
    public void IgnoreFlagDoesNotAffectExplicitEndpoint()
    {
        Environment.SetEnvironmentVariable("AWS_IGNORE_CONFIGURED_ENDPOINT_URLS", "true");
        Assert.Equal("https://explicit.example.com", AwsWebIdentityEnvironment.ResolveStsEndpoint("https://explicit.example.com"));
    }

    [Fact]
    public void IgnoreFlagOtherValueIsNotIgnored()
    {
        Environment.SetEnvironmentVariable("AWS_IGNORE_CONFIGURED_ENDPOINT_URLS", "false");
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com");
        Assert.Equal("https://sts.example.com", AwsWebIdentityEnvironment.ResolveStsEndpoint(null));
    }

    [Fact]
    public void FromAwsEnvironmentRequiresTokenFile()
    {
        Assert.Throws<InvalidOperationException>(() => WebIdentityProvider.FromAwsEnvironment());
    }

    private const string StsResponse = """
        <AssumeRoleWithWebIdentityResponse><AssumeRoleWithWebIdentityResult><Credentials>
        <AccessKeyId>AK</AccessKeyId><SecretAccessKey>SK</SecretAccessKey><SessionToken>ST</SessionToken>
        <Expiration>2030-01-01T00:00:00Z</Expiration></Credentials></AssumeRoleWithWebIdentityResult></AssumeRoleWithWebIdentityResponse>
        """;

    [Fact]
    public async Task FromAwsEnvironmentKeepsPathAndSendsRoleAndToken()
    {
        Environment.SetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE", TempFile("jwt-token\n"));
        Environment.SetEnvironmentVariable("AWS_ROLE_ARN", "arn:aws:iam::123456789012:role/example");
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com/sts/ns/name");

        Uri? seen = null;
        using var handler = new MockHttpMessageHandler((req, resp) =>
        {
            seen = req.RequestUri;
            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(StsResponse);
        });
        handler.InnerHandler = new HttpClientHandler();
        using var caller = new HttpClient(handler, disposeHandler: false);

        var provider = WebIdentityProvider.FromAwsEnvironment(httpClient: caller);
        var creds = await provider.GetCredentialsAsync(CancellationToken.None);

        Assert.Equal("AK", creds.AccessKey);
        Assert.NotNull(seen);
        Assert.Equal("sts.example.com", seen.Host);
        Assert.Equal("/sts/ns/name", seen.AbsolutePath);
        Assert.Equal("jwt-token", QueryValue(seen, "WebIdentityToken")); // file contents are trimmed
        Assert.Contains("RoleArn=", seen.Query, StringComparison.Ordinal);

        // The caller's client must survive repeated calls (the provider disposes what it is given).
        await provider.GetCredentialsAsync(CancellationToken.None);
    }

    // T12
    [Fact]
    public async Task UnreadableCaBundleIsAnError()
    {
        Environment.SetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE", TempFile("t"));
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com");
        Environment.SetEnvironmentVariable("AWS_CA_BUNDLE", MissingFile());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await WebIdentityProvider.FromAwsEnvironment().GetCredentialsAsync(CancellationToken.None));
        Assert.Contains("AWS_CA_BUNDLE", ex.Message, StringComparison.Ordinal);
    }

    // T13
    [Fact]
    public async Task CaBundleWithoutCertificatesIsAnError()
    {
        Environment.SetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE", TempFile("t"));
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com");
        Environment.SetEnvironmentVariable("AWS_CA_BUNDLE", TempFile("not a certificate"));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await WebIdentityProvider.FromAwsEnvironment().GetCredentialsAsync(CancellationToken.None));
        Assert.Contains("AWS_CA_BUNDLE", ex.Message, StringComparison.Ordinal);
    }

    // T9
    [Fact]
    public async Task CaBundleIsTrustedInRealTlsHandshake()
    {
        using var server = TlsServer.Start();
        Environment.SetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE", TempFile("jwt"));
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", server.Url);
        Environment.SetEnvironmentVariable("AWS_CA_BUNDLE", TempFile(server.CertificatePem));
        var provider = WebIdentityProvider.FromAwsEnvironment();
        var creds = await provider.GetCredentialsAsync(CancellationToken.None);
        Assert.Equal("AK", creds.AccessKey);
        // read at retrieval time: removing the variable afterwards makes the same provider fail verification
        Environment.SetEnvironmentVariable("AWS_CA_BUNDLE", null);
        await AssertCertificateFailureAsync(provider);
    }

    private static string QueryValue(Uri uri, string name)
    {
        foreach (var pair in uri.Query.TrimStart('?').Split('&'))
        {
            var kv = pair.Split('=', 2);
            if (Uri.UnescapeDataString(kv[0]) == name) return Uri.UnescapeDataString(kv[1]);
        }
        throw new InvalidOperationException(name + " not found in " + uri.Query);
    }

    private static (HttpClient Client, Func<Uri> Seen) RecordingClient()
    {
        Uri? seen = null;
        var handler = new MockHttpMessageHandler((req, resp) =>
        {
            seen = req.RequestUri;
            resp.Content = new StringContent(StsResponse);
        });
        handler.InnerHandler = new HttpClientHandler();
        return (new HttpClient(handler, disposeHandler: true), () => seen!);
    }

    // A reset before certificate validation would also throw HttpRequestException, so require the TLS failure itself.
    private static async Task AssertCertificateFailureAsync(WebIdentityProvider provider)
    {
        var ex = await Assert.ThrowsAnyAsync<HttpRequestException>(async () => await provider.GetCredentialsAsync(CancellationToken.None));
        Assert.IsType<System.Security.Authentication.AuthenticationException>(ex.InnerException);
    }

    // T10
    [Fact]
    public async Task WithoutCaBundleTlsVerificationFails()
    {
        using var server = TlsServer.Start();
        Environment.SetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE", TempFile("jwt"));
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", server.Url);
        var provider = WebIdentityProvider.FromAwsEnvironment();
        var ex = await Assert.ThrowsAnyAsync<HttpRequestException>(async () => await provider.GetCredentialsAsync(CancellationToken.None));
        Assert.IsType<System.Security.Authentication.AuthenticationException>(ex.InnerException);
    }

    // T11
    [Fact]
    public async Task CallerClientIgnoresBundleAndIsNotModified()
    {
        using var server = TlsServer.Start();
        Environment.SetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE", TempFile("jwt"));
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", server.Url);
        Environment.SetEnvironmentVariable("AWS_CA_BUNDLE", TempFile(server.CertificatePem));
        using var caller = new HttpClient();
        var provider = WebIdentityProvider.FromAwsEnvironment(httpClient: caller);
        // the bundle would make this succeed; with a caller client it is ignored
        await AssertCertificateFailureAsync(provider);
        // and the caller's client is still usable (not disposed) after the provider used it
        await AssertCertificateFailureAsync(provider);

        // An unreadable bundle must not matter either: retrieval goes through the caller's client.
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com");
        Environment.SetEnvironmentVariable("AWS_CA_BUNDLE", MissingFile());
        var reached = false;
        using var handler = new MockHttpMessageHandler((_, resp) =>
        {
            reached = true;
            resp.Content = new StringContent(StsResponse);
        });
        handler.InnerHandler = new HttpClientHandler();
        using var recording = new HttpClient(handler, disposeHandler: false);
        var creds = await WebIdentityProvider.FromAwsEnvironment(httpClient: recording).GetCredentialsAsync(CancellationToken.None);
        Assert.True(reached);
        Assert.Equal("AK", creds.AccessKey);
    }

    [Fact]
    public async Task SessionNameIsSentWhenSet()
    {
        Environment.SetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE", TempFile("jwt"));
        Environment.SetEnvironmentVariable("AWS_ROLE_SESSION_NAME", "example-session");
        Uri? seen = null;
        using var handler = new MockHttpMessageHandler((req, resp) =>
        {
            seen = req.RequestUri;
            resp.Content = new StringContent(StsResponse);
        });
        handler.InnerHandler = new HttpClientHandler();
        using var caller = new HttpClient(handler, disposeHandler: false);
        await WebIdentityProvider.FromAwsEnvironment(httpClient: caller).GetCredentialsAsync(CancellationToken.None);
        Assert.Equal("example-session", QueryValue(seen!, "RoleSessionName"));
    }

    // T5: the host is asserted from the request a recording handler sees; nothing is sent to the network.
    [Fact]
    public async Task IgnoreFlagSendsRequestToDefaultHost()
    {
        Environment.SetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE", TempFile("jwt"));
        Environment.SetEnvironmentVariable("AWS_IGNORE_CONFIGURED_ENDPOINT_URLS", "true");
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "https://sts.example.com");
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL", "https://global.example.com");
        Uri? seen = null;
        using var handler = new MockHttpMessageHandler((req, resp) =>
        {
            seen = req.RequestUri;
            resp.Content = new StringContent(StsResponse);
        });
        handler.InnerHandler = new HttpClientHandler();
        using var caller = new HttpClient(handler, disposeHandler: false);
        await WebIdentityProvider.FromAwsEnvironment(httpClient: caller).GetCredentialsAsync(CancellationToken.None);
        Assert.Equal("sts.amazonaws.com", seen!.Host);
    }

    [Fact]
    public void EmptyVariablesCountAsUnset()
    {
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_STS", "");
        Assert.Equal("https://sts.amazonaws.com", AwsWebIdentityEnvironment.ResolveStsEndpoint(null));
    }

    private sealed class TlsServer : IDisposable
    {
        private readonly System.Net.Sockets.TcpListener _listener;
        private readonly X509Certificate2 _cert;
        private readonly Task _loop;

        public string CertificatePem { get; }
        public string Url { get; }

        private TlsServer(System.Net.Sockets.TcpListener listener, X509Certificate2 cert, string pem, string url)
        {
            _listener = listener;
            _cert = cert;
            CertificatePem = pem;
            Url = url;
            _loop = Task.Run(Serve);
        }

        public static TlsServer Start()
        {
            using var key = RSA.Create(2048);
            var req = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            req.CertificateExtensions.Add(san.Build());
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyCertSign, true));
            req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.1")], false));
            using var self = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            var cert = LoadCert(self.Export(X509ContentType.Pfx));
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            return new TlsServer(listener, cert, self.ExportCertificatePem(), $"https://localhost:{port}/sts");
        }

        private async Task Serve()
        {
            try
            {
                while (true)
                {
                    using var tcp = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    try
                    {
                        using var ssl = new System.Net.Security.SslStream(tcp.GetStream());
                        await ssl.AuthenticateAsServerAsync(_cert).ConfigureAwait(false);
                        var buf = new byte[4096];
                        _ = await ssl.ReadAsync(buf).ConfigureAwait(false);
                        var body = System.Text.Encoding.UTF8.GetBytes(StsResponse);
                        var head = System.Text.Encoding.ASCII.GetBytes(
                            $"HTTP/1.1 200 OK\r\nContent-Type: application/xml\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
                        await ssl.WriteAsync(head).ConfigureAwait(false);
                        await ssl.WriteAsync(body).ConfigureAwait(false);
                        await ssl.FlushAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is IOException or System.Security.Authentication.AuthenticationException or ObjectDisposedException)
                    {
                        // client rejected the certificate; expected in T10/T11
                    }
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException or System.Net.Sockets.SocketException or InvalidOperationException)
            {
                // listener stopped
            }
        }

        public void Dispose()
        {
            _listener.Stop();
            _loop.Wait(TimeSpan.FromSeconds(5));
            _cert.Dispose();
        }
    }

    [Fact]
    public void ValidCaBundleLoadsAllCertificates()
    {
        using var ca1 = CreateCa("CN=ca1.example.com");
        using var ca2 = CreateCa("CN=ca2.example.com");
        var path = TempFile(ca1.ExportCertificatePem() + "\n" + ca2.ExportCertificatePem());
        Assert.Equal(2, AwsWebIdentityEnvironment.LoadCaBundle(path).Count);
    }

    [Fact]
    public void ValidationAcceptsChainEndingInBundleCa()
    {
        using var ca = CreateCa("CN=ca.example.com");
        using var leaf = CreateLeaf(ca, "sts.example.com");
        var bundle = new X509Certificate2Collection(ca);
        Assert.True(AwsWebIdentityEnvironment.ValidateWithBundle(leaf, null, SslPolicyErrors.RemoteCertificateChainErrors, bundle));
    }

    [Fact]
    public void ValidationRejectsChainFromOtherCa()
    {
        using var ca = CreateCa("CN=ca.example.com");
        using var other = CreateCa("CN=other.example.com");
        using var leaf = CreateLeaf(other, "sts.example.com");
        var bundle = new X509Certificate2Collection(ca);
        Assert.False(AwsWebIdentityEnvironment.ValidateWithBundle(leaf, null, SslPolicyErrors.RemoteCertificateChainErrors, bundle));
    }

    [Fact]
    public void ValidationRejectsLeafWithoutServerAuthentication()
    {
        using var ca = CreateCa("CN=ca.example.com");
        using var clientOnly = CreateLeaf(ca, "sts.example.com", "1.3.6.1.5.5.7.3.2");
        using var serverOk = CreateLeaf(ca, "sts.example.com", "1.3.6.1.5.5.7.3.1");
        var bundle = new X509Certificate2Collection(ca);
        Assert.False(AwsWebIdentityEnvironment.ValidateWithBundle(clientOnly, null, SslPolicyErrors.RemoteCertificateChainErrors, bundle));
        Assert.True(AwsWebIdentityEnvironment.ValidateWithBundle(serverOk, null, SslPolicyErrors.RemoteCertificateChainErrors, bundle));
    }

    [Fact]
    public void CallerClientWrapperDoesNotImposeItsOwnTimeout()
    {
        using var caller = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        using var wrapped = AwsWebIdentityEnvironment.CreateFactory(caller, useCaBundle: true).CreateClient("Minio");
        Assert.Equal(Timeout.InfiniteTimeSpan, wrapped.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(120), caller.Timeout);
    }

    [Fact]
    public async Task AwsRequestUsesAwsParameterNamesAndDefaultSessionName()
    {
        Environment.SetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE", TempFile("jwt"));
        Environment.SetEnvironmentVariable("AWS_ROLE_ARN", "arn:aws:iam::123456789012:role/example");
        Uri? seen = null;
        using var handler = new MockHttpMessageHandler((req, resp) =>
        {
            seen = req.RequestUri;
            resp.Content = new StringContent(StsResponse);
        });
        handler.InnerHandler = new HttpClientHandler();
        using var caller = new HttpClient(handler, disposeHandler: false);
        await WebIdentityProvider.FromAwsEnvironment(httpClient: caller).GetCredentialsAsync(CancellationToken.None);
        var q = seen!.Query;
        Assert.Contains("&RoleArn=arn%3Aaws%3Aiam%3A%3A123456789012%3Arole%2Fexample", "&" + q.TrimStart('?'), StringComparison.Ordinal);
        Assert.DoesNotContain("RoleARN=", q, StringComparison.Ordinal);
        var session = QueryValue(seen, "RoleSessionName");
        Assert.InRange(session.Length, 2, 64);
        Assert.Matches("^[\\w+=,.@-]+$", session);
    }

    [Fact]
    public async Task NonAwsCallersKeepTheMinioRequestContract()
    {
        Uri? seen = null;
        using var handler = new MockHttpMessageHandler((req, resp) =>
        {
            seen = req.RequestUri;
            resp.Content = new StringContent(StsResponse);
        });
        handler.InnerHandler = new HttpClientHandler();
        var factory = new TestHttpClientFactory(handler);
        var provider = new WebIdentityProvider(factory, new StaticAccessTokenProvider("jwt"),
            Microsoft.Extensions.Options.Options.Create(new WebIdentityCredentialsOptions { StsEndPoint = "https://sts.example.com", RoleARN = "arn:x" }));
        await provider.GetCredentialsAsync(CancellationToken.None);
        Assert.Contains("RoleARN=", seen!.Query, StringComparison.Ordinal);
        Assert.DoesNotContain("RoleSessionName", seen.Query, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationNeverAcceptsNameMismatch()
    {
        using var ca = CreateCa("CN=ca.example.com");
        using var leaf = CreateLeaf(ca, "sts.example.com");
        var bundle = new X509Certificate2Collection(ca);
        Assert.False(AwsWebIdentityEnvironment.ValidateWithBundle(
            leaf, null, SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch, bundle));
    }

    [Fact]
    public void ValidationKeepsDefaultTrust()
    {
        using var cert = CreateCa("CN=default-trust.example.com");
        var bundle = new X509Certificate2Collection();
        Assert.True(AwsWebIdentityEnvironment.ValidateWithBundle(cert, null, SslPolicyErrors.None, bundle));
        Assert.False(AwsWebIdentityEnvironment.ValidateWithBundle(null, null, SslPolicyErrors.RemoteCertificateNotAvailable, bundle));
    }

    // X509CertificateLoader exists from .NET 9 on; the constructor is obsolete there.
    private static X509Certificate2 LoadCert(byte[] data) =>
#if NET9_0_OR_GREATER
        X509CertificateLoader.LoadPkcs12(data, null);
#else
        new(data);
#endif

    private static X509Certificate2 CreateCa(string subject)
    {
        using var key = RSA.Create(2048);
        var req = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return LoadCert(cert.Export(X509ContentType.Pfx));
    }

    private static X509Certificate2 CreateLeaf(X509Certificate2 ca, string host, string ekuOid = "1.3.6.1.5.5.7.3.1")
    {
        using var key = RSA.Create(2048);
        var req = new CertificateRequest("CN=" + host, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid(ekuOid)], false));
        var serial = new byte[8];
        RandomNumberGenerator.Fill(serial);
        serial[0] &= 0x7F;
        using var cert = req.Create(ca, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(10), serial);
        return LoadCert(cert.Export(X509ContentType.Pfx));
    }
}
