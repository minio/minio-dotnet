using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio.Helpers;
using Minio.Model;
using Minio.Model.Notification;

#if NET6_0
using ArgumentException = Shims.ArgumentException; 
using ArgumentNullException = Shims.ArgumentNullException; 
using SHA256 = Shims.SHA256; 
using MD5 = Shims.MD5;
#else
using System.Security.Cryptography;
#endif

namespace Minio.Implementation;

internal class MinioClient : IMinioClient
{
    private static readonly XNamespace Ns = Constants.S3Ns;
    
    private const string EmptySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private static readonly Regex ExpirationRegex = new("expiry-date=\"(.*?)\", rule-id=\"(.*?)\"", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex RestoreRegex = new("ongoing-request=\"(.*?)\"(, expiry-date=\"(.*?)\")?", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly string[] PreserveKeys = new[]
    {
        "Content-Type",
        "Cache-Control",
        "Content-Encoding",
        "Content-Language",
        "Content-Disposition",
        "X-Amz-Storage-Class",
        "X-Amz-Object-Lock-Mode",
        "X-Amz-Object-Lock-Retain-Until-Date",
        "X-Amz-Object-Lock-Legal-Hold",
        "X-Amz-Website-Redirect-Location",
        "X-Amz-Server-Side-Encryption",
        "X-Amz-Tagging-Count",
        "X-Amz-Meta-",
    };
    
    private const long MaxMultipartPutObjectSize = 5L * 1024 * 1024 * 1024 * 1024; // 5TiB
    private const long MinPartSize = 16 * 1024 * 1024;  // 16MiB

    private readonly IOptions<ClientOptions> _options;
    private readonly ITimeProvider _timeProvider;
    private readonly IRequestAuthenticator _requestAuthenticator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MinioClient> _logger;
    private int _requestId;

    public MinioClient(IOptions<ClientOptions> options, ITimeProvider timeProvider, IRequestAuthenticator requestAuthenticator, IHttpClientFactory httpClientFactory, ILogger<MinioClient> logger)
    {
        _options = options;
        _timeProvider = timeProvider;
        _requestAuthenticator = requestAuthenticator;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> CreateBucketAsync(string bucketName, bool objectLocking, string region, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        
        var xml = new XElement(Ns + "CreateBucketConfiguration");
        if (!string.IsNullOrEmpty(region) && region != "us-east-1")
        {
            xml.Add(new XElement(Ns + "Location",
                new XElement(Ns + "Name", region)));
        }

        using var req = CreateRequest(HttpMethod.Put, bucketName, xml);
        if (objectLocking)
            req.Headers.Add("X-Amz-Bucket-Object-Lock-Enabled", "true");

        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
        return resp.GetHeaderValue("Location") ?? "";
    }

    public async Task DeleteBucketAsync(string bucketName, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        
        using var req = CreateRequest(HttpMethod.Delete, bucketName);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        
        try
        {
            using var req = CreateRequest(HttpMethod.Head, bucketName);
            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (MinioHttpException exc) when (exc.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async IAsyncEnumerable<BucketInfo> ListBucketsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var req = CreateRequest(HttpMethod.Get, string.Empty);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        var buckets = xResponse.Root?.Element(Ns + "Buckets");
        if (buckets != null)
        {
            foreach (var xContent in buckets.Elements(Ns + "Bucket"))
            {
                yield return new BucketInfo
                {
                    CreationDate = xContent.Element(Ns + "CreationDate")?.Value.ParseIsoTimestamp() ?? DateTimeOffset.UnixEpoch,
                    Name = xContent.Element(Ns + "Name")?.Value ?? string.Empty,
                };
            }
        }
    }

    public async Task<IDictionary<string, string>?> GetBucketTaggingAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        VerifyBucketName(bucketName);
        
        var query = new QueryParams();
        query.Add("tagging", string.Empty);

        try
        {
            using var req = CreateRequest(HttpMethod.Get, bucketName, query);
            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var tags = new Dictionary<string, string>();
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

                var xTags = xResponse.Root!.Element("TagSet")?.Elements("Tag");
                if (xTags != null)
                {
                    foreach (var xTag in xTags)
                    {
                        var key = xTag.Element("Key")?.Value;
                        if (!string.IsNullOrEmpty(key))
                        {
                            var value = xTag.Element("Value")?.Value ?? string.Empty;
                            tags.Add(key, value);
                        }
                    }
                }
            }

            return tags;
        }
        catch (MinioHttpException minioHttpException) when (minioHttpException.Error?.Code == "NoSuchTagSet")
        {
            // No tags set (different from an empty tag set)
            return null;
        }
    }

    public async Task SetBucketTaggingAsync(string bucketName, IEnumerable<KeyValuePair<string, string>>? tags, CancellationToken cancellationToken = default)
    {
        VerifyBucketName(bucketName);
        
        var query = new QueryParams();
        query.Add("tagging", string.Empty);

        if (tags != null)
        {
            var xTagSet = new XElement(Ns + "TagSet");
            foreach (var (key, value) in tags)
                xTagSet.Add(new XElement(Ns + "Tag",
                    new XElement(Ns + "Key", key),
                    new XElement(Ns + "Value", value)));

            var xTagging = new XElement(Ns + "Tagging", xTagSet);

            using var req = CreateRequest(HttpMethod.Put, bucketName, xTagging, query);
            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            using var req = CreateRequest(HttpMethod.Delete, bucketName, query);
            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<CreateMultipartUploadResult> CreateMultipartUploadAsync(string bucketName, string key, CreateMultipartUploadOptions? options, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        
        var query = new QueryParams();
        query.Add("uploads", string.Empty);
        
        using var req = CreateRequest(HttpMethod.Post, Encode(bucketName, key), query);

        req
            .SetContentType(options?.ContentType)
            .SetContentEncoding(options?.ContentEncoding)
            .SetContentDisposition(options?.ContentDisposition)
            .SetContentLanguage(options?.ContentLanguage)
            .SetCacheControl(options?.CacheControl)
            .SetExpires(options?.Expires)
            .SetObjectLockMode(options?.Mode)
            .SetObjectLockRetainUntilDate(options?.RetainUntilDate)
            .SetObjectLockLegalHold(options?.LegalHold)
            .SetStorageClass(options?.StorageClass)
            .SetWebsiteRedirectLocation(options?.WebsiteRedirectLocation)
            .SetTagging(options?.UserTags);

        options?.ServerSideEncryption?.WriteHeaders(req.Headers);
    
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var abortDate = DateTimeOffset.TryParseExact(resp.Headers.TryGetValue("X-Amz-Abort-Date"), "R", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ad) ? (DateTimeOffset?)ad : null;
        var abortRuleId = resp.Headers.TryGetValue("X-Amz-Abort-Rule-Id");
        
        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        return new CreateMultipartUploadResult
        {
            Bucket = xResponse.Root?.Element(Ns + "Bucket")?.Value ?? bucketName,
            Key = xResponse.Root?.Element(Ns + "Key")?.Value ?? key,
            UploadId = xResponse.Root?.Element(Ns + "UploadId")?.Value ?? string.Empty,
            AbortDate = abortDate,
            AbortRuleId = abortRuleId,
            CreateOptions = options
        };
    }

    public async Task<UploadPartResult> UploadPartAsync(string bucketName, string key, string uploadId, int partNumber, Stream stream, UploadPartOptions? options, ProgressHandler? progress, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(uploadId);
        if (partNumber < 1) throw new ArgumentOutOfRangeException(nameof(partNumber), "Part numbers start at 1");

        var query = new QueryParams();
        query.Add("partNumber", partNumber.ToString(CultureInfo.InvariantCulture));
        query.Add("uploadId", uploadId);
        
        using var req = CreateRequest(HttpMethod.Put, Encode(bucketName, key), query);

        if (progress != null)
            stream = new ProgressReadStream(stream, progress);

        req.Content = new StreamContent(stream);
        req
            .SetContentMD5(options?.ContentMD5)
            .SetChecksum(options?.ChecksumAlgorithm, options?.Checksum);
    
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        return new UploadPartResult
        {
            Etag = resp.Headers.TryGetValue("ETag"),
            ChecksumCRC32 = resp.Headers.TryGetValue("x-amz-checksum-crc32"),
            ChecksumCRC32C = resp.Headers.TryGetValue("x-amz-checksum-crc32c"),
            ChecksumSHA1 = resp.Headers.TryGetValue("x-amz-checksum-sha1"),
            ChecksumSHA256 = resp.Headers.TryGetValue("Checksumsha256"),
        };
    }

    public async Task<CompleteMultipartUploadResult> CompleteMultipartUploadAsync(string bucketName, string key, string uploadId, IEnumerable<PartInfo> parts, CompleteMultipartUploadOptions? options, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(uploadId);
        
        var query = new QueryParams();
        query.Add("uploadId", uploadId);

        var xml = new XElement(Ns + "CompleteMultipartUploadResult");
        var partNumber = 1;
        foreach (var part in parts)
        {
            var xPart = new XElement(Ns + "Part",
                new XElement(Ns + "PartNumber", partNumber++),
                new XElement(Ns + "ETag", part.Etag));

            if (part.ChecksumAlgorithm != null && part.Checksum != null)
            {
                var (header, length) = part.ChecksumAlgorithm switch
                {
                    ChecksumAlgorithm.Crc32 => (Ns + "ChecksumCRC32", 32),
                    ChecksumAlgorithm.Crc32c => (Ns + "ChecksumCRC32C", 32),
                    ChecksumAlgorithm.Sha1 => (Ns + "ChecksumSHA1", 128),
                    ChecksumAlgorithm.Sha256 => (Ns + "ChecksumSHA256", 256),
                    _ => throw new ArgumentException("Invalid checksum algorithm", nameof(parts))
                };
                if (part.Checksum.Length * 8 != length)
                    throw new ArgumentException($"Expected {length}-bit checksum", nameof(parts));
                xPart.Add(new XElement(header, Convert.ToBase64String(part.Checksum)));
            }

            xml.Add(xPart);
        }

        using var req = CreateRequest(HttpMethod.Post, Encode(bucketName, key), xml, query);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        return new CompleteMultipartUploadResult
        {
            Location = xResponse.Root?.Element(Ns + "Location")?.Value ?? string.Empty,
            Bucket = xResponse.Root?.Element(Ns + "Bucket")?.Value ?? string.Empty,
            Key = xResponse.Root?.Element(Ns + "Key")?.Value ?? string.Empty,
            Etag = xResponse.Root?.Element(Ns + "ETag")?.Value ?? string.Empty,
            ChecksumCRC32 = xResponse.Root?.Element(Ns + "ChecksumCRC32")?.Value,
            ChecksumCRC32C = xResponse.Root?.Element(Ns + "ChecksumCRC32C")?.Value,
            ChecksumSHA1 = xResponse.Root?.Element(Ns + "ChecksumSHA1")?.Value,
            ChecksumSHA256 = xResponse.Root?.Element(Ns + "ChecksumSHA256")?.Value
        };
    }
    
    public async Task AbortMultipartUploadAsync(string bucketName, string key, string uploadId, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(uploadId);

        var query = new QueryParams();
        query.Add("uploadId", uploadId);

        using var req = CreateRequest(HttpMethod.Delete, Encode(bucketName, key), query);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task PutObjectAsync(string bucketName, string key, Stream stream, PutObjectOptions? options, ProgressHandler? progress, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.Length > MaxMultipartPutObjectSize)
            throw new ArgumentOutOfRangeException(nameof(stream), stream.Length, "Stream length out of range");
    
        using var req = CreateRequest(HttpMethod.Put, Encode(bucketName, key));

        if (progress != null)
            stream = new ProgressReadStream(stream, progress);

        req.Content = new StreamContent(stream);
        req
            .SetIfMatchETag(options?.IfMatchETag)
            .SetIfMatchETagExcept(options?.IfMatchETagExcept)
            .SetContentType(options?.ContentType)
            .SetContentEncoding(options?.ContentEncoding)
            .SetContentDisposition(options?.ContentDisposition)
            .SetContentLanguage(options?.ContentLanguage)
            .SetCacheControl(options?.CacheControl)
            .SetExpires(options?.Expires)
            .SetObjectLockMode(options?.Mode)
            .SetObjectLockRetainUntilDate(options?.RetainUntilDate)
            .SetObjectLockLegalHold(options?.LegalHold)
            .SetStorageClass(options?.StorageClass)
            .SetWebsiteRedirectLocation(options?.WebsiteRedirectLocation)
            .SetTagging(options?.UserTags)
            .SetUserMetadata(options?.UserMetadata);
            
        options?.ServerSideEncryption?.WriteHeaders(req.Headers);
    
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ObjectInfo> HeadObjectAsync(string bucketName, string key, GetObjectOptions? options = null, CancellationToken cancellationToken = default)
    {
        var resp = await GetOrHeadObjectAsync(HttpMethod.Head, bucketName, key, options, cancellationToken).ConfigureAwait(false);
        return ToObjectInfo(key, resp);
    }

    public async Task<ObjectInfoStream> GetObjectAsync(string bucketName, string key, GetObjectOptions? options, CancellationToken cancellationToken)
    {
        var resp = await GetOrHeadObjectAsync(HttpMethod.Get, bucketName, key, options, cancellationToken).ConfigureAwait(false);
        var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var objectInfo = ToObjectInfo(key, resp);
        return new ObjectInfoStream(stream, objectInfo, resp);
    }

    public async Task DeleteObjectAsync(string bucketName, string key, string? versionId, bool bypassGovernanceRetention, string? expectedBucketOwner, string? mfa, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        
        var q = new QueryParams();
        q.AddIfNotNullOrEmpty("versionId", versionId);

        using var req = CreateRequest(HttpMethod.Delete, Encode(bucketName, key), q);
        req.SetBypassGovernanceRetention(bypassGovernanceRetention)
            .SetExpectedBucketOwner(expectedBucketOwner)
            .SetMfa(mfa);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteObjectsAsync(string bucketName, IEnumerable<ObjectIdentifier> objects, bool bypassGovernanceRetention, string? expectedBucketOwner, string? mfa, CancellationToken cancellationToken)
    {
        var deleteObjects = InternalDeleteObjectsAsync(bucketName, objects, bypassGovernanceRetention, expectedBucketOwner, mfa, true, cancellationToken);
        await foreach (var _ in deleteObjects.ConfigureAwait(false))
        {
            // We should never receive any results in quiet mode
        }
    }
    
    public IAsyncEnumerable<DeleteResult> DeleteObjectsVerboseAsync(string bucketName, IEnumerable<ObjectIdentifier> objects, bool bypassGovernanceRetention, string? expectedBucketOwner, string? mfa, CancellationToken cancellationToken)
    {
        return InternalDeleteObjectsAsync(bucketName, objects, bypassGovernanceRetention, expectedBucketOwner, mfa, false, cancellationToken);
    }
    
    private async IAsyncEnumerable<DeleteResult> InternalDeleteObjectsAsync(string bucketName, IEnumerable<ObjectIdentifier> objects, bool bypassGovernanceRetention, string? expectedBucketOwner, string? mfa, bool quiet, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentNullException.ThrowIfNull(objects);

        var q = new QueryParams();
        q.Add("delete", string.Empty);

        // Ensure it's batched in max 1000 items
        var itemsLeft = true;
        using var enumerator = objects.GetEnumerator();
        while (itemsLeft)
        {
            var xDelete = new XElement(Ns + "Delete");
            if (quiet)
                xDelete.Add(new XElement(Ns + "Quiet", true));

            var items = 0;
            while (items < 1000)
            {
                itemsLeft = enumerator.MoveNext();
                if (!itemsLeft) break;
                
                var obj = enumerator.Current;
                var xObject = new XElement(Ns + "Object", new XElement(Ns + "Key", obj.Key));
                if (obj.VersionId != null)
                    xObject.Add(new XElement(Ns + "VersionId", obj.VersionId));
                if (!string.IsNullOrEmpty(obj.ETag))
                    xObject.Add(new XElement(Ns + "ETag", obj.ETag));
                if (obj.LastModifiedTime != null)
                    xObject.Add(new XElement(Ns + "LastModifiedTime", obj.LastModifiedTime));
                if (obj.Size != null)
                    xObject.Add(new XElement(Ns + "Size", obj.Size));
                xDelete.Add(xObject);
                ++items;
            }

            if (items > 0)
            {
                HttpResponseMessage resp;
                using (var req = CreateRequest(HttpMethod.Post, bucketName, xDelete, q))
                {
                    await AddContentMd5Async(req, cancellationToken).ConfigureAwait(false);

                    req.SetBypassGovernanceRetention(bypassGovernanceRetention)
                        .SetExpectedBucketOwner(expectedBucketOwner)
                        .SetMfa(mfa);
                    resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
                }

                using (resp)
                {
                    if (!quiet)
                    {
                        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

                        foreach (var xResult in xResponse.Root!.Elements().Where(x => x.Name.Namespace == Ns))
                        {
                            switch (xResult.Name.LocalName)
                            {
                                case "Deleted":
                                {
                                    var key = xResult.Element(Ns + "Key")?.Value ?? string.Empty;
                                    var versionIdText = xResult.Element(Ns + "VersionId")?.Value;
                                    var versionId = !string.IsNullOrEmpty(versionIdText) ? versionIdText : null;
                                    var deleteMarkerText = xResult.Element(Ns + "DeleteMarker")?.Value;
                                    var deleteMarker = deleteMarkerText != null ? bool.TryParse(deleteMarkerText, out var dm) ? (bool?)dm : null : null;
                                    var deleteMarkerVersionIdText = xResult.Element(Ns + "DeleteMarkerVersionId")?.Value;
                                    var deleteMarkerVersionId = !string.IsNullOrEmpty(deleteMarkerVersionIdText) ? deleteMarkerVersionIdText : null;
                                    yield return new DeleteResult(key, versionId, deleteMarker, deleteMarkerVersionId);
                                    break;
                                }
                                case "Error":
                                {
                                    var key = xResult.Element(Ns + "Key")?.Value ?? string.Empty;
                                    var versionIdText = xResult.Element(Ns + "VersionId")?.Value;
                                    var versionId = !string.IsNullOrEmpty(versionIdText) ? versionIdText : null;
                                    var errorCodeText = xResult.Element(Ns + "Code")?.Value;
                                    var errorCode = !string.IsNullOrEmpty(errorCodeText) ? errorCodeText : null;
                                    var errorMessageText = xResult.Element(Ns + "Message")?.Value;
                                    var errorMessage = !string.IsNullOrEmpty(errorMessageText) ? errorMessageText : null;
                                    yield return new DeleteResult(key, versionId, ErrorCode: errorCode, ErrorMessage: errorMessage);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private async Task<HttpResponseMessage> GetOrHeadObjectAsync(HttpMethod httpMethod, string bucketName, string key, GetObjectOptions? options, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var q = new QueryParams();
        q.AddIfNotNullOrEmpty("versionId", options?.VersionId);
        if (options?.PartNumber != null)
            q.AddIfNotNullOrEmpty("partNumber", options.PartNumber.Value.ToString(CultureInfo.InvariantCulture));
        using var req = CreateRequest(httpMethod, Encode(bucketName, key), q);

        options?.ServerSideEncryption?.WriteHeaders(req.Headers);
        if (options?.CheckSum ?? false)
            req.Headers.Add("x-amz-checksum-mode", "ENABLED");
        if (options?.IfMatchETag != null)
        {
            if (string.IsNullOrEmpty(options.IfMatchETag)) throw new ArgumentException(nameof(options.IfMatchETag) + " should not be empty", nameof(options));
            req.Headers.Add("If-Match", '"' + options.IfMatchETag + '"');
        }
        if (options?.IfMatchETagExcept != null)
        {
            if (string.IsNullOrEmpty(options.IfMatchETagExcept)) throw new ArgumentException(nameof(options.IfMatchETagExcept) + " should not be empty", nameof(options));
            req.Headers.Add("If-None-Match", '"' + options.IfMatchETagExcept + '"');
        }
        req.Headers.AddIfNotNull("If-Unmodified-Since", options?.IfUnmodifiedSince?.ToIsoTimestamp());
        req.Headers.AddIfNotNull("If-Modified-Since", options?.IfModifiedSince?.ToIsoTimestamp());
        if (options?.Range != null)
        {
            var range = options.Range.Value;
            var rangeHeaderValue = range.Start switch
            {
                0 when range.End < 0 => $"bytes={range.End}",
                > 0 when range.End == 0 => $"bytes={range.Start}-",
                >= 0 when range.Start < range.End => $"bytes={range.Start}-{range.End}",
                _ => throw new ArgumentException("Invalid range", nameof(options))
            };
            req.Headers.Add("Range", rangeHeaderValue);
        }

        return await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<ObjectItem> ListObjectsAsync(string bucketName, string? continuationToken, string? delimiter, bool includeMetadata, string? fetchOwner, int pageSize, string? prefix, string? startAfter, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        while (true)
        {
            var q = new QueryParams();
            q.Add("list-type", "2");
            q.Add("encoding-type", "url");
            q.AddIfNotNullOrEmpty("continuation-token", continuationToken);
            q.AddIfNotNullOrEmpty("delimiter", delimiter);
            if (includeMetadata)
                q.Add("metadata", "true");
            q.AddIfNotNullOrEmpty("fetch-owner", fetchOwner);
            if (pageSize > 0)
                q.Add("max-keys", pageSize.ToString(CultureInfo.InvariantCulture));
            q.AddIfNotNullOrEmpty("prefix", prefix);
            q.AddIfNotNullOrEmpty("start-after", startAfter);
            using var req = CreateRequest(HttpMethod.Get, bucketName, q);
            
            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            foreach (var xContent in xResponse.Root!.Elements(Ns + "Contents"))
            {
                MediaTypeHeaderValue? contentType = null;
                DateTimeOffset? expires = null;
                var userMetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (includeMetadata)
                {
                    var xUserMetadata = xContent.Element(Ns + "UserMetadata");
                    if (xUserMetadata == null)
                        throw new InvalidOperationException("Client doesn't support metadata while listing objects (MinIO specific feature)");

                    contentType = MediaTypeHeaderValue.TryParse(xUserMetadata.Element(Ns + "content-type")?.Value, out var ct) ? ct : null;
                    expires = DateTimeOffset.TryParseExact(xUserMetadata.Element(Ns + "expires")?.Value, "r", CultureInfo.InvariantCulture, DateTimeStyles.None, out var v) ? v : null;
                    const string metaElementPrefix = "X-Amz-Meta-";
                    foreach (var xHeader in xUserMetadata.Elements().Where(x => x.Name.Namespace == Ns && x.Name.LocalName.StartsWith(metaElementPrefix, StringComparison.OrdinalIgnoreCase)))
                    {
                        var key = xHeader.Name.LocalName[metaElementPrefix.Length..];
                        userMetaData[key] = xHeader.Value;
                    }
                }

                var objItem = new ObjectItem
                {
                    Key = Uri.UnescapeDataString(xContent.Element(Ns + "Key")?.Value ?? string.Empty),
                    ETag = xContent.Element(Ns + "ETag")?.Value ?? string.Empty,
                    Size = long.TryParse(xContent.Element(Ns + "Size")?.Value, out var size) ? size : -1,
                    StorageClass = xContent.Element(Ns + "StorageClass")?.Value ?? string.Empty,
                    LastModified = xContent.Element(Ns + "LastModified")?.Value.ParseIsoTimestamp() ?? DateTimeOffset.MinValue,
                    ContentType = contentType,
                    Expires = expires,
                    UserMetadata = userMetaData
                };

                yield return objItem;
            }
            
            continuationToken = xResponse.Root!.Element(Ns + "NextContinuationToken")?.Value;
            var isTruncated = xResponse.Root!.Element(Ns + "IsTruncated")?.Value == "true";
            if (!isTruncated) break;
        }
    }

    public async IAsyncEnumerable<PartItem> ListPartsAsync(string bucketName, string key, string uploadId, int pageSize, string? partNumberMarker, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        while (true)
        {
            var q = new QueryParams();
            if (pageSize > 0)
                q.Add("max-parts", pageSize.ToString(CultureInfo.InvariantCulture));
            q.AddIfNotNullOrEmpty("part-number-marker", partNumberMarker);
            q.AddIfNotNullOrEmpty("uploadId", uploadId);
            using var req = CreateRequest(HttpMethod.Get, Encode(bucketName, key), q);

            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            foreach (var xPart in xResponse.Root!.Elements(Ns + "Part"))
            {
                yield return new PartItem
                {
                    ETag = xPart.Element(Ns + "ETag")?.Value ?? string.Empty,
                    LastModified = DateTimeOffset.Parse(xPart.Element(Ns + "LastModified")?.Value ?? string.Empty, CultureInfo.InvariantCulture),
                    PartNumber = int.Parse(xPart.Element(Ns + "PartNumber")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Size = long.Parse(xPart.Element(Ns + "Size")?.Value ?? "0", CultureInfo.InvariantCulture),
                    ChecksumCRC32 = xPart.Element(Ns + "ChecksumCRC32")?.Value,
                    ChecksumCRC32C = xPart.Element(Ns + "ChecksumCRC32C")?.Value,
                    ChecksumSHA1 = xPart.Element(Ns + "ChecksumSHA1")?.Value,
                    ChecksumSHA256 = xPart.Element(Ns + "ChecksumSHA256")?.Value
                };
            }
            
            partNumberMarker = xResponse.Root!.Element(Ns + "NextPartNumberMarker")?.Value;
            var isTruncated = xResponse.Root!.Element(Ns + "IsTruncated")?.Value == "true";
            if (!isTruncated) break;
        }
    }

    public async IAsyncEnumerable<UploadItem> ListMultipartUploadsAsync(string bucketName, string? delimiter, string? encodingType, string? keyMarker, int pageSize, string? prefix, string? uploadIdMarker, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        while (true)
        {
            var q = new QueryParams();
            q.Add("uploads", string.Empty);
            q.AddIfNotNullOrEmpty("delimiter", delimiter);
            q.AddIfNotNullOrEmpty("encoding-type", encodingType);
            q.AddIfNotNullOrEmpty("key-marker", keyMarker);
            if (pageSize > 0)
                q.Add("max-uploads", pageSize.ToString(CultureInfo.InvariantCulture));
            q.AddIfNotNullOrEmpty("prefix", prefix);
            q.AddIfNotNullOrEmpty("upload-id-marker", uploadIdMarker);
            using var req = CreateRequest(HttpMethod.Get, bucketName, q);

            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            foreach (var xUpload in xResponse.Root!.Elements(Ns + "Upload"))
            {
                yield return new UploadItem
                {
                    UploadId = xUpload.Element(Ns + "UploadId")?.Value ?? string.Empty,
                    Key = xUpload.Element(Ns + "Key")?.Value ?? string.Empty,
                    Initiated = DateTimeOffset.Parse(xUpload.Element(Ns + "Initiated")?.Value ?? string.Empty, CultureInfo.InvariantCulture),
                    StorageClass = xUpload.Element(Ns + "StorageClass")?.Value ?? string.Empty,
                };
            }
            
            keyMarker = xResponse.Root!.Element(Ns + "NextKeyMarker")?.Value;
            uploadIdMarker = xResponse.Root!.Element(Ns + "NextUploadIdMarker")?.Value;
            var isTruncated = xResponse.Root!.Element(Ns + "IsTruncated")?.Value == "true";
            if (!isTruncated) break;
        }
    }

    public async Task<BucketNotification> GetBucketNotificationsAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        VerifyBucketName(bucketName);
        
        var query = new QueryParams();
        query.Add("notification", string.Empty);
        
        using var req = CreateRequest(HttpMethod.Get, bucketName, query);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        return BucketNotification.Deserialize(xResponse.Root!);
    }

    public async Task SetBucketNotificationsAsync(string bucketName, BucketNotification bucketNotification, CancellationToken cancellationToken = default)
    {
        VerifyBucketName(bucketName);
        
        var query = new QueryParams();
        query.Add("notification", string.Empty);

        var xml = bucketNotification.Serialize();
        
        using var req = CreateRequest(HttpMethod.Put, bucketName, xml, query);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IObservable<NotificationEvent>> ListenBucketNotificationsAsync(string bucketName, IEnumerable<EventType> events, string prefix, string suffix, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        if (events == null) throw new ArgumentNullException(nameof(events));

        var query = new QueryParams();
        query.Add("ping", "10");
        query.AddIfNotNullOrEmpty("prefix", prefix);
        query.AddIfNotNullOrEmpty("suffix", suffix);

        var hasEvents = false;
        foreach (var e in events)
        {
            hasEvents = true;
            query.Add("events", e.ToString());
        }

        if (!hasEvents)
            throw new ArgumentException("No events specified", nameof(events));

        using var req = CreateRequest(HttpMethod.Get, bucketName, query);
        var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
        try
        {
            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return Observable.Create<NotificationEvent>(async (obs, ct) =>
            {
                // ReSharper disable once AccessToDisposedClosure
                // This variable is only disposed when an exception occured,
                // so then this delegate won't be called.
                using (resp)
                await using (responseBody.ConfigureAwait(false))
                {
                    using var sr = new StreamReader(responseBody);
                    while (!sr.EndOfStream)
                    {
                        ct.ThrowIfCancellationRequested();
#if NET7_0_OR_GREATER
                        var line = await sr.ReadLineAsync(ct).ConfigureAwait(false);
#else
                    var line = await sr.ReadLineAsync().ConfigureAwait(false);
#endif
                        if (string.IsNullOrEmpty(line))
                            continue;

                        var bucketNotificationEvent = JsonSerializer.Deserialize<BucketNotificationEvent>(line);
                        if (bucketNotificationEvent?.Records != null)
                        {
                            foreach (var e in bucketNotificationEvent.Records)
                                obs.OnNext(e);
                        }
                    }
                    obs.OnCompleted();
                }
            }).SubscribeOn(TaskPoolScheduler.Default);
        }
        catch
        {
            resp.Dispose();
            throw;
        }
    }

    public async Task<ObjectLockConfiguration> GetObjectLockConfigurationAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        VerifyBucketName(bucketName);
        
        var query = new QueryParams();
        query.Add("object-lock", string.Empty);

        using var req = CreateRequest(HttpMethod.Get, bucketName, query);
        try
        {
            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            return ObjectLockConfiguration.Deserialize(xResponse.Root!);
        }
        catch (MinioHttpException exc) when (exc.Error?.Code == "ObjectLockConfigurationNotFoundError")
        {
            return new ObjectLockConfiguration();
        }
    }

    public async Task SetObjectLockConfigurationAsync(string bucketName, RetentionRule? defaultRetentionRule = null, CancellationToken cancellationToken = default)
    {
        VerifyBucketName(bucketName);
        
        var query = new QueryParams();
        query.Add("object-lock", string.Empty);

        var config = new ObjectLockConfiguration
        {
            DefaultRetentionRule = defaultRetentionRule
        };
        var xml = config.Serialize();
        
        using var req = CreateRequest(HttpMethod.Put, bucketName, xml, query);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }
    
    public async Task<VersioningConfiguration> GetBucketVersioningAsync(string bucketName, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        
        var query = new QueryParams();
        query.Add("versioning", string.Empty);

        using var req = CreateRequest(HttpMethod.Get, bucketName, query);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        return VersioningConfiguration.Deserialize(xResponse.Root!);
    }

    public async Task SetBucketVersioningAsync(string bucketName, VersioningStatus status, bool mfaDelete, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        var config = new VersioningConfiguration
        {
            Status = status,
            MfaDelete = mfaDelete,
        };
        var xml = config.Serialize();

        var query = new QueryParams();
        query.Add("versioning", string.Empty);

        using var req = CreateRequest(HttpMethod.Put, bucketName, xml, query);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BucketEncryptionConfiguration> GetBucketEncryptionAsync(string bucketName, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        var q = new QueryParams();
        q.Add("encryption", string.Empty);

        using var req = CreateRequest(HttpMethod.Get, bucketName, q);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        return BucketEncryptionConfiguration.Deserialize(xResponse.Root!);
    }

    public async Task SetBucketEncryptionAsync(string bucketName, BucketEncryptionConfiguration config, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentNullException.ThrowIfNull(config);

        var q = new QueryParams();
        q.Add("encryption", string.Empty);

        using var req = CreateRequest(HttpMethod.Put, bucketName, config.Serialize(), q);
        await AddContentMd5Async(req, cancellationToken).ConfigureAwait(false);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveBucketEncryptionAsync(string bucketName, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        var q = new QueryParams();
        q.Add("encryption", string.Empty);

        using var req = CreateRequest(HttpMethod.Delete, bucketName, q);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<LifecycleConfiguration?> GetBucketLifecycleAsync(string bucketName, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        var q = new QueryParams();
        q.Add("lifecycle", string.Empty);

        try
        {
            using var req = CreateRequest(HttpMethod.Get, bucketName, q);
            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);
            return LifecycleConfiguration.Deserialize(xResponse.Root!);
        }
        catch (MinioHttpException exc) when (exc.Error?.Code == "NoSuchLifecycleConfiguration")
        {
            return null;
        }
    }

    public async Task SetBucketLifecycleAsync(string bucketName, LifecycleConfiguration config, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentNullException.ThrowIfNull(config);

        var q = new QueryParams();
        q.Add("lifecycle", string.Empty);

        using var req = CreateRequest(HttpMethod.Put, bucketName, config.Serialize(), q);
        await AddContentMd5Async(req, cancellationToken).ConfigureAwait(false);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveBucketLifecycleAsync(string bucketName, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        var q = new QueryParams();
        q.Add("lifecycle", string.Empty);

        using var req = CreateRequest(HttpMethod.Delete, bucketName, q);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReplicationConfiguration?> GetBucketReplicationAsync(string bucketName, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        var q = new QueryParams();
        q.Add("replication", string.Empty);

        try
        {
            using var req = CreateRequest(HttpMethod.Get, bucketName, q);
            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);
            return ReplicationConfiguration.Deserialize(xResponse.Root!);
        }
        catch (MinioHttpException exc) when (exc.Error?.Code == "ReplicationConfigurationNotFoundError")
        {
            return null;
        }
    }

    public async Task SetBucketReplicationAsync(string bucketName, ReplicationConfiguration config, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentNullException.ThrowIfNull(config);

        var q = new QueryParams();
        q.Add("replication", string.Empty);

        using var req = CreateRequest(HttpMethod.Put, bucketName, config.Serialize(), q);
        await AddContentMd5Async(req, cancellationToken).ConfigureAwait(false);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveBucketReplicationAsync(string bucketName, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        var q = new QueryParams();
        q.Add("replication", string.Empty);

        using var req = CreateRequest(HttpMethod.Delete, bucketName, q);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> GetPolicyAsync(string bucketName, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        var q = new QueryParams();
        q.Add("policy", string.Empty);

        try
        {
            using var req = CreateRequest(HttpMethod.Get, bucketName, q);
            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
            return await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (MinioHttpException exc) when (exc.Error?.Code == "NoSuchBucketPolicy")
        {
            return null;
        }
    }

    public async Task SetPolicyAsync(string bucketName, string policy, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(policy);

        var q = new QueryParams();
        q.Add("policy", string.Empty);

        using var req = CreateRequest(HttpMethod.Put, bucketName, q);
        req.Content = new StringContent(policy, Encoding.UTF8, "application/json");
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemovePolicyAsync(string bucketName, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);

        var q = new QueryParams();
        q.Add("policy", string.Empty);

        using var req = CreateRequest(HttpMethod.Delete, bucketName, q);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<LegalHoldStatus> GetObjectLegalHoldAsync(string bucketName, string key, string? versionId, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var q = new QueryParams();
        q.Add("legal-hold", string.Empty);
        q.AddIfNotNullOrEmpty("versionId", versionId);

        using var req = CreateRequest(HttpMethod.Get, Encode(bucketName, key), q);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        var statusText = xResponse.Root?.Element(Ns + "Status")?.Value;
        return statusText == "ON" ? LegalHoldStatus.On : LegalHoldStatus.Off;
    }

    public async Task SetObjectLegalHoldAsync(string bucketName, string key, LegalHoldStatus status, string? versionId, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var q = new QueryParams();
        q.Add("legal-hold", string.Empty);
        q.AddIfNotNullOrEmpty("versionId", versionId);

        var xml = new XElement(Ns + "LegalHold",
            new XElement(Ns + "Status", status == LegalHoldStatus.On ? "ON" : "OFF"));

        using var req = CreateRequest(HttpMethod.Put, Encode(bucketName, key), xml, q);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ObjectRetention> GetObjectRetentionAsync(string bucketName, string key, string? versionId, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var q = new QueryParams();
        q.Add("retention", string.Empty);
        q.AddIfNotNullOrEmpty("versionId", versionId);

        using var req = CreateRequest(HttpMethod.Get, Encode(bucketName, key), q);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        return ObjectRetention.Deserialize(xResponse.Root!);
    }

    public async Task SetObjectRetentionAsync(string bucketName, string key, ObjectRetention retention, bool bypassGovernanceRetention, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(retention);

        var q = new QueryParams();
        q.Add("retention", string.Empty);

        using var req = CreateRequest(HttpMethod.Put, Encode(bucketName, key), retention.Serialize(), q);
        req.SetBypassGovernanceRetention(bypassGovernanceRetention);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearObjectRetentionAsync(string bucketName, string key, string? versionId, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var q = new QueryParams();
        q.Add("retention", string.Empty);
        q.AddIfNotNullOrEmpty("versionId", versionId);

        var xml = new XElement(Ns + "Retention");
        using var req = CreateRequest(HttpMethod.Put, Encode(bucketName, key), xml, q);
        req.Headers.Add("X-Amz-Bypass-Governance-Retention", "true");
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CopyObjectResult> CopyObjectAsync(string destBucketName, string destKey, string srcBucketName, string srcKey, CopyObjectOptions? options, CancellationToken cancellationToken)
    {
        VerifyBucketName(destBucketName);
        ArgumentException.ThrowIfNullOrEmpty(destKey);
        VerifyBucketName(srcBucketName);
        ArgumentException.ThrowIfNullOrEmpty(srcKey);

        using var req = CreateRequest(HttpMethod.Put, Encode(destBucketName, destKey));

        // Build the copy source header
        var copySource = "/" + Encode(srcBucketName, srcKey);
        if (!string.IsNullOrEmpty(options?.SourceVersionId))
            copySource += "?versionId=" + Uri.EscapeDataString(options.SourceVersionId);
        req.Headers.Add("X-Amz-Copy-Source", copySource);

        if (options?.MetadataDirective != null)
            req.Headers.Add("X-Amz-Metadata-Directive",
                options.MetadataDirective == MetadataDirective.Replace ? "REPLACE" : "COPY");

        if (options?.TaggingDirective != null)
            req.Headers.Add("X-Amz-Tagging-Directive",
                options.TaggingDirective == TaggingDirective.Replace ? "REPLACE" : "COPY");

        if (options?.IfMatch != null)
            req.Headers.Add("X-Amz-Copy-Source-If-Match", '"' + options.IfMatch + '"');
        if (options?.IfNoneMatch != null)
            req.Headers.Add("X-Amz-Copy-Source-If-None-Match", '"' + options.IfNoneMatch + '"');
        if (options?.IfModifiedSince != null)
            req.Headers.Add("X-Amz-Copy-Source-If-Modified-Since", options.IfModifiedSince.Value.ToString("R"));
        if (options?.IfUnmodifiedSince != null)
            req.Headers.Add("X-Amz-Copy-Source-If-Unmodified-Since", options.IfUnmodifiedSince.Value.ToString("R"));

        if (options?.MetadataDirective == MetadataDirective.Replace)
        {
            req
                .SetContentType(options.ContentType)
                .SetContentEncoding(options.ContentEncoding)
                .SetContentDisposition(options.ContentDisposition)
                .SetContentLanguage(options.ContentLanguage)
                .SetCacheControl(options.CacheControl)
                .SetExpires(options.Expires)
                .SetUserMetadata(options.UserMetadata);
        }
        if (options?.TaggingDirective == TaggingDirective.Replace)
            req.SetTagging(options.UserTags);

        req
            .SetStorageClass(options?.StorageClass)
            .SetWebsiteRedirectLocation(options?.WebsiteRedirectLocation)
            .SetObjectLockMode(options?.Mode)
            .SetObjectLockRetainUntilDate(options?.RetainUntilDate)
            .SetObjectLockLegalHold(options?.LegalHold);

        options?.ServerSideEncryption?.WriteHeaders(req.Headers);

        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var versionId = resp.Headers.TryGetValue("X-Amz-Version-Id");
        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        return new CopyObjectResult
        {
            ETag = xResponse.Root?.Element(Ns + "ETag")?.Value ?? string.Empty,
            LastModified = DateTimeOffset.Parse(
                xResponse.Root?.Element(Ns + "LastModified")?.Value ?? string.Empty,
                CultureInfo.InvariantCulture),
            VersionId = versionId,
            ChecksumCRC32 = xResponse.Root?.Element(Ns + "ChecksumCRC32")?.Value,
            ChecksumCRC32C = xResponse.Root?.Element(Ns + "ChecksumCRC32C")?.Value,
            ChecksumSHA1 = xResponse.Root?.Element(Ns + "ChecksumSHA1")?.Value,
            ChecksumSHA256 = xResponse.Root?.Element(Ns + "ChecksumSHA256")?.Value,
        };
    }

    public async IAsyncEnumerable<string> SelectObjectContentAsync(string bucketName, string key, SelectObjectOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(options);

        var q = new QueryParams();
        q.Add("select", string.Empty);
        q.Add("select-type", "2");

        using var req = CreateRequest(HttpMethod.Post, Encode(bucketName, key), options.Serialize(), q);
        var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
        try
        {
            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await foreach (var record in ReadSelectEventStreamAsync(responseBody, cancellationToken).ConfigureAwait(false))
            {
                yield return record;
            }
        }
        finally
        {
            resp.Dispose();
        }
    }

    private static async IAsyncEnumerable<string> ReadSelectEventStreamAsync(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var prelude = new byte[12];
        while (true)
        {
            if (!await ReadExactAsync(stream, prelude, cancellationToken).ConfigureAwait(false))
                yield break;

            var totalLength = BinaryPrimitives.ReadInt32BigEndian(prelude.AsSpan(0, 4));
            var headersLength = BinaryPrimitives.ReadInt32BigEndian(prelude.AsSpan(4, 4));
            // Bytes 8-11 are the prelude CRC (not validated here)

            var headerBytes = new byte[headersLength];
            await ReadExactAsync(stream, headerBytes, cancellationToken).ConfigureAwait(false);

            var payloadLength = totalLength - headersLength - 16; // 12 prelude + 4 message CRC
            var payload = payloadLength > 0 ? new byte[payloadLength] : Array.Empty<byte>();
            if (payloadLength > 0)
                await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);

            // Skip the 4-byte message CRC
            var crcBuffer = new byte[4];
            await ReadExactAsync(stream, crcBuffer, cancellationToken).ConfigureAwait(false);

            var headers = ParseEventStreamHeaders(headerBytes);

            if (headers.TryGetValue(":message-type", out var messageType) && messageType == "error")
            {
                headers.TryGetValue(":error-code", out var errorCode);
                headers.TryGetValue(":error-message", out var errorMessage);
                throw new InvalidOperationException($"S3 Select error {errorCode}: {errorMessage}");
            }

            if (headers.TryGetValue(":event-type", out var eventType))
            {
                if (eventType == "End")
                    yield break;
                if (eventType == "Records" && payloadLength > 0)
                    yield return Encoding.UTF8.GetString(payload);
            }
        }
    }

    private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                if (offset == 0) return false;
                throw new EndOfStreamException("Unexpected end of S3 Select event stream.");
            }
            offset += read;
        }
        return true;
    }

    private static Dictionary<string, string> ParseEventStreamHeaders(byte[] headerBytes)
    {
        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        var i = 0;
        while (i < headerBytes.Length)
        {
            var nameLen = headerBytes[i++];
            var name = Encoding.UTF8.GetString(headerBytes, i, nameLen);
            i += nameLen;
            var valueType = headerBytes[i++];
            switch (valueType)
            {
                case 0: case 1: break; // bool true/false, no data
                case 2: i += 1; break; // byte
                case 3: i += 2; break; // short
                case 4: i += 4; break; // int
                case 5: case 8: i += 8; break; // long, timestamp
                case 9: i += 16; break; // UUID
                case 6: // bytes (2-byte length + data)
                {
                    var len = BinaryPrimitives.ReadInt16BigEndian(headerBytes.AsSpan(i));
                    i += 2 + len;
                    break;
                }
                case 7: // string (2-byte length + UTF-8 data)
                {
                    var len = BinaryPrimitives.ReadInt16BigEndian(headerBytes.AsSpan(i));
                    i += 2;
                    headers[name] = Encoding.UTF8.GetString(headerBytes, i, len);
                    i += len;
                    break;
                }
            }
        }
        return headers;
    }

    public async Task<Uri> PresignedGetObjectAsync(string bucketName, string key, TimeSpan expiry, string? versionId, IDictionary<string, string>? responseHeaders, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var q = new QueryParams();
        q.AddIfNotNullOrEmpty("versionId", versionId);
        if (responseHeaders != null)
            foreach (var (k, v) in responseHeaders)
                q.Add(k, v);

        using var req = CreateRequest(HttpMethod.Get, Encode(bucketName, key), q);
        return await _requestAuthenticator.PresignAsync(req, _options.Value.Region, "s3", expiry, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Uri> PresignedPutObjectAsync(string bucketName, string key, TimeSpan expiry, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        using var req = CreateRequest(HttpMethod.Put, Encode(bucketName, key));
        return await _requestAuthenticator.PresignAsync(req, _options.Value.Region, "s3", expiry, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PostPolicyResult> PresignedPostPolicyAsync(string bucketName, string key, TimeSpan expiry, IEnumerable<PostPolicyCondition>? conditions, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        Uri bucketUri;
        using (var req = CreateRequest(HttpMethod.Post, bucketName))
            bucketUri = req.RequestUri!;

        return await _requestAuthenticator.PresignPostPolicyAsync(bucketUri, bucketName, key, expiry, _options.Value.Region, "s3", conditions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IDictionary<string, string>?> GetObjectTagsAsync(string bucketName, string key, string? versionId, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var q = new QueryParams();
        q.Add("tagging", string.Empty);
        q.AddIfNotNullOrEmpty("versionId", versionId);

        try
        {
            using var req = CreateRequest(HttpMethod.Get, Encode(bucketName, key), q);
            using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var tags = new Dictionary<string, string>();
            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            var xTags = xResponse.Root!.Element("TagSet")?.Elements("Tag");
            if (xTags != null)
            {
                foreach (var xTag in xTags)
                {
                    var tagKey = xTag.Element("Key")?.Value;
                    if (!string.IsNullOrEmpty(tagKey))
                        tags[tagKey] = xTag.Element("Value")?.Value ?? string.Empty;
                }
            }
            return tags;
        }
        catch (MinioHttpException exc) when (exc.Error?.Code == "NoSuchTagSet")
        {
            return null;
        }
    }

    public async Task SetObjectTagsAsync(string bucketName, string key, IEnumerable<KeyValuePair<string, string>> tags, string? versionId, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(tags);

        var q = new QueryParams();
        q.Add("tagging", string.Empty);
        q.AddIfNotNullOrEmpty("versionId", versionId);

        var xTagSet = new XElement(Ns + "TagSet");
        foreach (var (k, v) in tags)
            xTagSet.Add(new XElement(Ns + "Tag",
                new XElement(Ns + "Key", k),
                new XElement(Ns + "Value", v)));

        var xTagging = new XElement(Ns + "Tagging", xTagSet);
        using var req = CreateRequest(HttpMethod.Put, Encode(bucketName, key), xTagging, q);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveObjectTagsAsync(string bucketName, string key, string? versionId, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var q = new QueryParams();
        q.Add("tagging", string.Empty);
        q.AddIfNotNullOrEmpty("versionId", versionId);

        using var req = CreateRequest(HttpMethod.Delete, Encode(bucketName, key), q);
        using var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task UploadObjectAsync(string bucketName, string key, Stream stream, PutObjectOptions? options, ProgressHandler? progress, CancellationToken cancellationToken)
    {
        VerifyBucketName(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(stream);

        switch (stream.Length)
        {
            case > MaxMultipartPutObjectSize:
                throw new ArgumentOutOfRangeException(nameof(stream), stream.Length, "Stream length out of range");
            case <= MinPartSize:
                await PutObjectAsync(bucketName, key, stream, options, progress, cancellationToken).ConfigureAwait(false);
                return;
        }

        // Determine part size: aim for at most 10,000 parts
        var partSize = Math.Max(MinPartSize, (stream.Length + 9999) / 10000);

        var createOptions = options == null ? null : new CreateMultipartUploadOptions
        {
            ContentType = options.ContentType,
            ContentEncoding = options.ContentEncoding,
            ContentDisposition = options.ContentDisposition,
            ContentLanguage = options.ContentLanguage,
            CacheControl = options.CacheControl,
            Expires = options.Expires,
            ServerSideEncryption = options.ServerSideEncryption,
            StorageClass = options.StorageClass,
            WebsiteRedirectLocation = options.WebsiteRedirectLocation,
            Mode = options.Mode,
            RetainUntilDate = options.RetainUntilDate,
            LegalHold = options.LegalHold,
        };
        if (options?.UserTags != null)
        {
            foreach (var kv in options.UserTags)
                (createOptions!.UserTags as Dictionary<string, string>)![kv.Key] = kv.Value;
        }

        var createResult = await CreateMultipartUploadAsync(bucketName, key, createOptions, cancellationToken).ConfigureAwait(false);
        var uploadId = createResult.UploadId;
        var parts = new List<PartInfo>();

        try
        {
            var partNumber = 1;
            var buffer = new byte[partSize];
            var totalBytesUploaded = 0L;
            var streamLength = stream.Length;

            while (true)
            {
                using var partStream = new MemoryStream();
                var bytesRead = 0L;
                while (bytesRead < partSize)
                {
                    var toRead = (int)Math.Min(buffer.Length, partSize - bytesRead);
                    var read = await stream.ReadAsync(buffer, 0, toRead, cancellationToken).ConfigureAwait(false);
                    if (read == 0) break;
                    await partStream.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                    bytesRead += read;
                }

                if (bytesRead == 0) break;
                partStream.Position = 0;

                ProgressHandler? partProgress = null;
                if (progress != null)
                {
                    var capturedOffset = totalBytesUploaded;
                    partProgress = (pos, _) => progress(capturedOffset + pos, streamLength);
                }

                var partResult = await UploadPartAsync(bucketName, key, uploadId, partNumber, partStream, null, partProgress, cancellationToken).ConfigureAwait(false);
                parts.Add(new PartInfo { Etag = partResult.Etag ?? string.Empty });
                totalBytesUploaded += bytesRead;
                partNumber++;
            }

            await CompleteMultipartUploadAsync(bucketName, key, uploadId, parts, null, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await AbortMultipartUploadAsync(bucketName, key, uploadId, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage req, CancellationToken cancellationToken)
    {
        if (req.Content != null)
        {
            var stream = await req.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            if (!stream.CanSeek)
                throw new ArgumentException("Request content stream must be seekable for SHA-256 signing.", nameof(req));
            var hashSha256 = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            req.Headers.Add("X-Amz-Content-Sha256", hashSha256.ToHexStringLowercase());
            stream.Position = 0;
        }
        else
        {
            req.Headers.Add("X-Amz-Content-Sha256", EmptySha256);
        }

        req.Headers.Add("X-Amz-Date", _timeProvider.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));
        await _requestAuthenticator.AuthenticateAsync(req, _options.Value.Region, "s3", cancellationToken).ConfigureAwait(false);
        
        using var httpClient = _httpClientFactory.CreateClient(_options.Value.MinioHttpClient);
        var requestId = Interlocked.Increment(ref _requestId);
        var sw = Stopwatch.StartNew();
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Request #{RequestID} {Method} {Url}", requestId, req.Method, req.RequestUri);

        try
        {
            var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                using (resp)
                {
                    var responseData = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var contentType = resp.Content.Headers.ContentType?.MediaType;
                    if (contentType == "application/xml" && !string.IsNullOrEmpty(responseData))
                    {
                        var xRoot = XDocument.Parse(responseData).Root;
                        if (xRoot != null)
                        {
                            var err = new ErrorResponse
                            {
                                Code = xRoot.Element("Code")?.Value ?? string.Empty,
                                Message = xRoot.Element("Message")?.Value ?? string.Empty,
                                BucketName = xRoot.Element("BucketName")?.Value ?? string.Empty,
                                Key = xRoot.Element("Key")?.Value ?? string.Empty,
                                Resource = xRoot.Element("Resource")?.Value ?? string.Empty,
                                RequestId = xRoot.Element("RequestId")?.Value ?? string.Empty,
                                HostId = xRoot.Element("HostId")?.Value ?? string.Empty,
                                Region = xRoot.Element("Region")?.Value ?? string.Empty,
                                Server = xRoot.Element("Server")?.Value ?? string.Empty,
                            };
                            if (_logger.IsEnabled(LogLevel.Debug))
                                _logger.LogDebug("Response #{RequestID} failed {StatusCode} - {Code}: {Message} ({Duration})", requestId, resp.StatusCode, err.Code, err.Message, sw.Elapsed);
                            throw new MinioHttpException(req, resp, err);
                        }
                    }

                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug("Response #{RequestID} failed {StatusCode} ({Duration})", requestId, resp.StatusCode, sw.Elapsed);
                    throw new MinioHttpException(req, resp, null);
                }
            }

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Response #{RequestID} {StatusCode} ({Duration})", requestId, resp.StatusCode, sw.Elapsed);
            return resp;
        }
        catch (Exception exc) when (exc is not MinioHttpException)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(exc, "Response #{RequestID} threw an exception ({Duration})", requestId, sw.Elapsed);
            throw;
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, QueryParams? queryParameters = null)
    {
        var uriBuilder = new StringBuilder();
        uriBuilder.Append(_options.Value.EndPoint);
        if (uriBuilder[^1] != '/')
            uriBuilder.Append('/');
        if (!string.IsNullOrEmpty(path))
            uriBuilder.Append(path);
        if (queryParameters != null)
            uriBuilder.Append(queryParameters);

        return new HttpRequestMessage(method, new Uri(uriBuilder.ToString()));
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, XElement xml, QueryParams? queryParameters = null)
    {
        var req = CreateRequest(method, path, queryParameters);
        req.Content = new XmlHttpContent(new XDocument(xml));
        return req;
    }

    private static async Task AddContentMd5Async(HttpRequestMessage req, CancellationToken cancellationToken)
    {
        var stream = await req.Content!.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        if (!stream.CanSeek)
            throw new InvalidOperationException("Cannot compute Content-MD5: the request content stream is not seekable.");
        stream.Position = 0;
        req.Content.Headers.ContentMD5 = await MD5.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        stream.Position = 0;
    }

    private static void VerifyBucketName(string bucketName, [CallerArgumentExpression("bucketName")] string? paramName = null)
    {
        if (bucketName == null) throw new ArgumentNullException(paramName);
        if (!VerificationHelpers.VerifyBucketName(bucketName))
            throw new ArgumentException("Invalid bucket name", paramName);
    }

    private static string Encode(string bucketName, string key)
    {
        var sb = new StringBuilder();
        sb.Append(bucketName);
        foreach (var keyPart in key.Split('/'))
        {
            sb.Append('/');
            sb.Append(Uri.EscapeDataString(keyPart));
        }

        return sb.ToString();
    }

    private static ObjectInfo ToObjectInfo(string key, HttpResponseMessage resp)
    {
        var etag = resp.Headers.ETag!;
        var contentLength = resp.Content.Headers.ContentLength!;
        var lastModified = resp.Content.Headers.LastModified;
        var contentType = resp.Content.Headers.ContentType ?? new MediaTypeHeaderValue("application/octet-stream");
        var expires = resp.Content.Headers.Expires;
        var versionId = resp.Headers.TryGetValue("X-Amz-Version-Id");
        var replicationStatus = resp.Headers.TryGetValue("X-Amz-Replication-Status");
        
        // Headers are case-insensitive, so the metadata
        var metadata =
            resp.Headers
                .Where(kv => PreserveKeys.Any(k => kv.Key.StartsWith(k, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(
                    kv => kv.Key, 
                    kv => string.Join(",", kv.Value),
                    StringComparer.OrdinalIgnoreCase);

        const string metaPrefix = "X-Amz-Meta-"; 
        var userMetadata = 
            metadata
                .Where(kv => kv.Key.StartsWith(metaPrefix, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    kv => kv.Key[metaPrefix.Length..], 
                    kv => string.Join(",", kv.Value),
                    StringComparer.OrdinalIgnoreCase);
        var userTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in resp.Headers.TryGetValues("X-Amz-Tagging"))
        {
            var qs = HttpUtility.ParseQueryString(value);
            foreach (var k in qs.AllKeys)
            {
                if (k != null)
                    userTags[k] = qs[k] ?? string.Empty;
            }
        }
        var tagCount = int.Parse(resp.Headers.TryGetValue("X-Amz-Tagging-Count") ?? "0", CultureInfo.InvariantCulture);
        var restoreValue = resp.Headers.TryGetValue("X-Amz-Restore");
        Restore? restore = null;
        if (!string.IsNullOrEmpty(restoreValue))
        {
            var matches = RestoreRegex.Matches(restoreValue);
            if (matches.Count == 4)
            {
                var ongoingRestore = bool.Parse(matches[1].Value);
                var restoreExpiryDate = !string.IsNullOrEmpty(matches[3].Value) ? (DateTimeOffset?)DateTimeOffset.ParseExact(matches[3].Value, "R", CultureInfo.InvariantCulture) : null;
                restore = new Restore(ongoingRestore, restoreExpiryDate);
            }
        }

        var expirationValue = resp.Headers.TryGetValue("X-Amz-Expiration");
        DateTimeOffset? expirationDate = null;
        string? expirationRuleId = null;
        if (!string.IsNullOrEmpty(expirationValue))
        {
            var matches = ExpirationRegex.Matches(expirationValue);
            if (matches.Count == 3)
            {
                expirationDate = DateTimeOffset.ParseExact(matches[1].Value, "R", CultureInfo.InvariantCulture);
                expirationRuleId = matches[2].Value;
            }
            
        }
        
        var deleteMarker = resp.Headers.TryGetValue("X-Amz-Delete-Marker") == "true";
        
        return new ObjectInfo
        {
            Etag = etag,
            Key = key,
            ContentLength = contentLength,
            LastModified = lastModified,
            ContentType = contentType,
            Expires = expires,
            VersionId = versionId,
            IsDeleteMarker = deleteMarker,
            ReplicationStatus = replicationStatus,
            Expiration = expirationDate,
            ExpirationRuleId = expirationRuleId,
            
            Metadata = metadata,
            UserMetadata = userMetadata,
            UserTags = userTags,
            UserTagCount = tagCount,
            Restore = restore,

            // Checksum values
            ChecksumCRC32 = resp.Headers.TryGetValue("x-amz-checksum-crc32"),
            ChecksumCRC32C = resp.Headers.TryGetValue("x-amz-checksum-crc32c"),
            ChecksumSHA1 = resp.Headers.TryGetValue("x-amz-checksum-sha1"),
            ChecksumSHA256 = resp.Headers.TryGetValue("x-amz-checksum-sha256"),
        };
    }
}
