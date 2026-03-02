using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Minio.Model;

namespace Minio.Helpers;

internal static class HttpHeadersExtensions
{
    public static void AddIfNotNull(this HttpHeaders headers, string key, string? value)
    {
        if (value != null)
            headers.Add(key, value);
    }

    public static IEnumerable<string> TryGetValues(this HttpHeaders headers, string key)
    {
        return headers.TryGetValues(key, out var values) ? values : Array.Empty<string>(); 
    }
    public static string? TryGetValue(this HttpHeaders headers, string key)
    {
        return headers.TryGetValues(key, out var values) ? string.Join(",", values) : null;
    }

    public static HttpRequestMessage SetIfMatchETag(this HttpRequestMessage req, string? etag)
    {
        if (etag != null)
            req.Headers.Add("If-Match", '"' + etag + '"');
        return req;
    }

    public static HttpRequestMessage SetIfMatchETagExcept(this HttpRequestMessage req, string? etag)
    {
        if (etag != null)
            req.Headers.Add("If-None-Match", '"' + etag + '"');
        return req;
    }

    public static HttpRequestMessage SetContentMD5(this HttpRequestMessage req, byte[]? contentMD5)
    {
        if (contentMD5 == null) return req;
        if (contentMD5.Length * 8 != 128) throw new ArgumentException("MD5 should be a 128-bit value", nameof(contentMD5));
        req.Content ??= new ByteArrayContent(Array.Empty<byte>());
        req.Content.Headers.ContentMD5 = contentMD5;
        return req;
    }
    
    public static HttpRequestMessage SetChecksum(this HttpRequestMessage req, ChecksumAlgorithm? algorithm, byte[]? checksum)
    {
        if (algorithm == null || checksum == null) return req;
        var (header, length) = algorithm.Value switch
        {
            ChecksumAlgorithm.Crc32 => ("x-amz-checksum-crc32", 32),
            ChecksumAlgorithm.Crc32c => ("x-amz-checksum-crc32c", 32),
            ChecksumAlgorithm.Sha1 => ("x-amz-checksum-sha1", 128),
            ChecksumAlgorithm.Sha256 => ("x-amz-checksum-sha256", 256),
            _ => throw new ArgumentException("Invalid checksum algorithm", nameof(algorithm))
        };
        if (checksum.Length * 8 != length)
            throw new ArgumentException($"Expected {length}-bit checksum", nameof(checksum));
        req.Headers.Add(header, Convert.ToBase64String(checksum));
        return req;
    }

    public static HttpRequestMessage SetContentType(this HttpRequestMessage req, MediaTypeHeaderValue? contentType)
    {
        if (contentType == null) return req;
        req.Content ??= new ByteArrayContent(Array.Empty<byte>());
        req.Content.Headers.ContentType = contentType;
        return req;
    }

    public static HttpRequestMessage SetContentEncoding(this HttpRequestMessage req, IEnumerable<string>? contentEncodings)
    {
        if (contentEncodings == null) return req;
        req.Content ??= new ByteArrayContent(Array.Empty<byte>());
        foreach (var contentEncoding in contentEncodings)
            req.Content.Headers.ContentEncoding.Add(contentEncoding);
        return req;
    }

    public static HttpRequestMessage SetContentDisposition(this HttpRequestMessage req, ContentDispositionHeaderValue? contentDisposition)
    {
        if (contentDisposition == null) return req;
        req.Content ??= new ByteArrayContent(Array.Empty<byte>());
        req.Content.Headers.ContentDisposition = contentDisposition;
        return req;
    }

    public static HttpRequestMessage SetContentLanguage(this HttpRequestMessage req, IEnumerable<string>? contentLanguages)
    {
        if (contentLanguages == null) return req;
        req.Content ??= new ByteArrayContent(Array.Empty<byte>());
        foreach (var contentLanguage in contentLanguages)
            req.Content.Headers.ContentLanguage.Add(contentLanguage);
        return req;
    }

    public static HttpRequestMessage SetCacheControl(this HttpRequestMessage req, CacheControlHeaderValue? cacheControl)
    {
        if (cacheControl == null) return req;
        req.Headers.CacheControl = cacheControl;
        return req;
    }

    public static HttpRequestMessage SetExpires(this HttpRequestMessage req, DateTimeOffset? expires)
    {
        if (expires == null) return req;
        // TODO: It looks like .NET doesn't allow to set the Expires header on request messages
        req.Headers.AddIfNotNull("Expires", expires.Value.ToString("R"));   
        return req;
    }
    
    public static HttpRequestMessage SetObjectLockMode(this HttpRequestMessage req, RetentionMode? mode)
    {
        var modeHeaderValue = mode switch
        {
            null => null,
            RetentionMode.Compliance => RetentionModeExtensions.Serialize(mode.Value),
            RetentionMode.Governance => RetentionModeExtensions.Serialize(mode.Value),
            _ => throw new ArgumentException("Invalid object lock mode", nameof(mode))
        };
        req.Headers.AddIfNotNull("X-Amz-Object-Lock-Mode", modeHeaderValue);
        return req;
    }
    
    public static HttpRequestMessage SetObjectLockRetainUntilDate(this HttpRequestMessage req, DateTimeOffset? date)
    {
        req.Headers.AddIfNotNull("X-Amz-Object-Lock-Retain-Until-Date", date?.ToUniversalTime().ToIsoTimestamp());
        return req;
    }
    
    public static HttpRequestMessage SetObjectLockLegalHold(this HttpRequestMessage req, LegalHoldStatus? legalHold)
    {
        var legalHoldHeaderValue = legalHold switch
        {
            null => null,
            LegalHoldStatus.On => "ON",
            LegalHoldStatus.Off => "OFF",
            _ => throw new ArgumentException("Invalid legal hold status", nameof(legalHold))
        };
        req.Headers.AddIfNotNull("X-Amz-Object-Lock-Legal-Hold", legalHoldHeaderValue);
        return req;
    }
    
    public static HttpRequestMessage SetStorageClass(this HttpRequestMessage req, string? setStorageClass)
    {
        req.Headers.AddIfNotNull("X-Amz-Storage-Class", setStorageClass);
        return req;
    }
    
    public static HttpRequestMessage SetWebsiteRedirectLocation(this HttpRequestMessage req, string? websiteRedirectLocation)
    {
        req.Headers.AddIfNotNull("X-Amz-Website-Redirect-Location", websiteRedirectLocation);
        return req;
    }
    
    public static HttpRequestMessage SetTagging(this HttpRequestMessage req, IEnumerable<KeyValuePair<string,string>>? tags)
    {
        if (tags is null) return req;
        var q = new QueryParams();
        var hasTags = false;
        foreach (var (k, v) in tags)
        {
            q.Add(k, v);
            hasTags = true;
        }
        if (hasTags)
            req.Headers.Add("X-Amz-Tagging", q.ToString()[1..]);
        return req;
    }
    
    public static HttpRequestMessage SetUserMetadata(this HttpRequestMessage req, IEnumerable<KeyValuePair<string,string>>? metadata)
    {
        if (metadata is null) return req;
        foreach (var (k, v) in metadata)
            req.Headers.Add("X-Amz-Meta-" + k, v);
        return req;
    }
    
    public static HttpRequestMessage SetMfa(this HttpRequestMessage req, string? mfa)
    {
        if (mfa != null)
            req.Headers.Add("X-Amz-MFA", mfa);
        return req;
    }
    
    public static HttpRequestMessage SetBypassGovernanceRetention(this HttpRequestMessage req, bool bypassGovernanceRetention)
    {
        if (bypassGovernanceRetention)
            req.Headers.Add("X-Amz-Bypass-Governance-Retention", "true");
        return req;
    }
    
    public static HttpRequestMessage SetExpectedBucketOwner(this HttpRequestMessage req, string? expectedBucketOwner)
    {
        if (expectedBucketOwner != null)
            req.Headers.Add("X-Amz-Expected-Bucket-Owner", expectedBucketOwner);
        return req;
    }
}
