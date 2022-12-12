/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2020 MinIO, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Minio.Credentials;
using Minio.DataModel;
using Minio.DataModel.ILM;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Replication;
using Minio.DataModel.Tags;
using Minio.Exceptions;

namespace Minio;

public interface IMinioClient : IDisposable
{
    Task<bool> BucketExistsAsync(BucketExistsArgs args, CancellationToken cancellationToken = default);
    Task ClearObjectRetentionAsync(ClearObjectRetentionArgs args, CancellationToken cancellationToken = default);
    Task CopyObjectAsync(CopyObjectArgs args, CancellationToken cancellationToken = default);

    Task<ServerSideEncryptionConfiguration> GetBucketEncryptionAsync(GetBucketEncryptionArgs args,
        CancellationToken cancellationToken = default);

    Task<LifecycleConfiguration> GetBucketLifecycleAsync(GetBucketLifecycleArgs args,
        CancellationToken cancellationToken = default);

    Task<BucketNotification> GetBucketNotificationsAsync(GetBucketNotificationsArgs args,
        CancellationToken cancellationToken = default);

    Task<ReplicationConfiguration> GetBucketReplicationAsync(GetBucketReplicationArgs args,
        CancellationToken cancellationToken = default);

    Task<Tagging> GetBucketTagsAsync(GetBucketTagsArgs args, CancellationToken cancellationToken = default);
    Task<ObjectStat> GetObjectAsync(GetObjectArgs args, CancellationToken cancellationToken = default);
    Task<bool> GetObjectLegalHoldAsync(GetObjectLegalHoldArgs args, CancellationToken cancellationToken = default);

    Task<ObjectLockConfiguration> GetObjectLockConfigurationAsync(GetObjectLockConfigurationArgs args,
        CancellationToken cancellationToken = default);

    Task<ObjectRetentionConfiguration> GetObjectRetentionAsync(GetObjectRetentionArgs args,
        CancellationToken cancellationToken = default);

    Task<Tagging> GetObjectTagsAsync(GetObjectTagsArgs args, CancellationToken cancellationToken = default);
    Task<string> GetPolicyAsync(GetPolicyArgs args, CancellationToken cancellationToken = default);

    Task<VersioningConfiguration> GetVersioningAsync(GetVersioningArgs args,
        CancellationToken cancellationToken = default);

    Task<ListAllMyBucketsResult> ListBucketsAsync(CancellationToken cancellationToken = default);

    IObservable<MinioNotificationRaw> ListenBucketNotificationsAsync(ListenBucketNotificationsArgs args,
        CancellationToken cancellationToken = default);

    IObservable<MinioNotificationRaw> ListenBucketNotificationsAsync(string bucketName, IList<EventType> events,
        string prefix = "", string suffix = "", CancellationToken cancellationToken = default);

    IObservable<Upload> ListIncompleteUploads(ListIncompleteUploadsArgs args,
        CancellationToken cancellationToken = default);

    IObservable<Item> ListObjectsAsync(ListObjectsArgs args, CancellationToken cancellationToken = default);
    Task MakeBucketAsync(MakeBucketArgs args, CancellationToken cancellationToken = default);
    Task<string> PresignedGetObjectAsync(PresignedGetObjectArgs args);
    Task<(Uri, Dictionary<string, string>)> PresignedPostPolicyAsync(PostPolicy policy);
    Task<(Uri, Dictionary<string, string>)> PresignedPostPolicyAsync(PresignedPostPolicyArgs args);
    Task<string> PresignedPutObjectAsync(PresignedPutObjectArgs args);
    Task PutObjectAsync(PutObjectArgs args, CancellationToken cancellationToken = default);

    Task RemoveAllBucketNotificationsAsync(RemoveAllBucketNotificationsArgs args,
        CancellationToken cancellationToken = default);

    Task RemoveBucketAsync(RemoveBucketArgs args, CancellationToken cancellationToken = default);
    Task RemoveBucketEncryptionAsync(RemoveBucketEncryptionArgs args, CancellationToken cancellationToken = default);
    Task RemoveBucketLifecycleAsync(RemoveBucketLifecycleArgs args, CancellationToken cancellationToken = default);
    Task RemoveBucketReplicationAsync(RemoveBucketReplicationArgs args, CancellationToken cancellationToken = default);
    Task RemoveBucketTagsAsync(RemoveBucketTagsArgs args, CancellationToken cancellationToken = default);
    Task RemoveIncompleteUploadAsync(RemoveIncompleteUploadArgs args, CancellationToken cancellationToken = default);
    Task RemoveObjectAsync(RemoveObjectArgs args, CancellationToken cancellationToken = default);

    Task RemoveObjectLockConfigurationAsync(RemoveObjectLockConfigurationArgs args,
        CancellationToken cancellationToken = default);

    Task<IObservable<DeleteError>> RemoveObjectsAsync(RemoveObjectsArgs args,
        CancellationToken cancellationToken = default);

    Task RemoveObjectTagsAsync(RemoveObjectTagsArgs args, CancellationToken cancellationToken = default);
    Task RemovePolicyAsync(RemovePolicyArgs args, CancellationToken cancellationToken = default);

    Task<SelectResponseStream> SelectObjectContentAsync(SelectObjectContentArgs args,
        CancellationToken cancellationToken = default);

    void SetAppInfo(string appName, string appVersion);
    Task SetBucketEncryptionAsync(SetBucketEncryptionArgs args, CancellationToken cancellationToken = default);
    Task SetBucketLifecycleAsync(SetBucketLifecycleArgs args, CancellationToken cancellationToken = default);
    Task SetBucketNotificationsAsync(SetBucketNotificationsArgs args, CancellationToken cancellationToken = default);
    Task SetBucketReplicationAsync(SetBucketReplicationArgs args, CancellationToken cancellationToken = default);
    Task SetBucketTagsAsync(SetBucketTagsArgs args, CancellationToken cancellationToken = default);
    Task SetObjectLegalHoldAsync(SetObjectLegalHoldArgs args, CancellationToken cancellationToken = default);

    Task SetObjectLockConfigurationAsync(SetObjectLockConfigurationArgs args,
        CancellationToken cancellationToken = default);

    Task SetObjectRetentionAsync(SetObjectRetentionArgs args, CancellationToken cancellationToken = default);
    Task SetObjectTagsAsync(SetObjectTagsArgs args, CancellationToken cancellationToken = default);
    Task SetPolicyAsync(SetPolicyArgs args, CancellationToken cancellationToken = default);
    void SetTraceOff();
    void SetTraceOn(IRequestLogger logger = null);
    Task SetVersioningAsync(SetVersioningArgs args, CancellationToken cancellationToken = default);
    Task<ObjectStat> StatObjectAsync(StatObjectArgs args, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> WrapperGetAsync(string url);
    Task WrapperPutAsync(string url, StreamContent strm);
}

public partial class MinioClient : IMinioClient
{
    internal IWebProxy Proxy { get; private set; }

    public MinioClient WithEndpoint(string endpoint)
    {
        BaseUrl = endpoint;
        SetBaseURL(GetBaseUrl(endpoint));
        return this;
    }

    public MinioClient WithEndpoint(string endpoint, int port)
    {
        if (port < 1 || port > 65535)
            throw new ArgumentException(string.Format("Port {0} is not a number between 1 and 65535", port), "port");
        return WithEndpoint(endpoint + ":" + port);
    }

    public MinioClient WithEndpoint(Uri url)
    {
        if (url == null) throw new ArgumentException("URL is null. Can't create endpoint.");
        return WithEndpoint(url.AbsoluteUri);
    }

    public MinioClient WithRegion(string region)
    {
        if (string.IsNullOrEmpty(region))
            throw new ArgumentException(string.Format("{0} the region value can't be null or empty.", region),
                "region");
        Region = region;
        return this;
    }

    public MinioClient WithCredentials(string accessKey, string secretKey)
    {
        AccessKey = accessKey;
        SecretKey = secretKey;
        return this;
    }

    public MinioClient WithSessionToken(string st)
    {
        SessionToken = st;
        return this;
    }

    public MinioClient Build()
    {
        // Instantiate a region cache
        regionCache = BucketRegionCache.Instance;
        if (string.IsNullOrEmpty(BaseUrl)) throw new MinioException("Endpoint not initialized.");
        if (Provider != null && Provider.GetType() != typeof(ChainedProvider) && SessionToken == null)
            throw new MinioException("User Access Credentials Provider not initialized correctly.");
        if (Provider == null && (string.IsNullOrEmpty(AccessKey) || string.IsNullOrEmpty(SecretKey)))
            throw new MinioException("User Access Credentials not initialized.");

        var host = BaseUrl;

        var scheme = Secure ? utils.UrlEncode("https") : utils.UrlEncode("http");

        if (!BaseUrl.StartsWith("http"))
            Endpoint = string.Format("{0}://{1}", scheme, host);
        else
            Endpoint = host;

        HTTPClient ??= Proxy is null ? new HttpClient() : new HttpClient(new HttpClientHandler { Proxy = Proxy });
        HTTPClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", FullUserAgent);
        return this;
    }

    private void SetBaseURL(Uri url)
    {
        if (url.IsDefaultPort)
            BaseUrl = url.Host;
        else
            BaseUrl = url.Host + ":" + url.Port;
    }

    private Uri GetBaseUrl(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentException(
                string.Format("{0} is the value of the endpoint. It can't be null or empty.", endpoint), "endpoint");
        if (endpoint.EndsWith("/")) endpoint = endpoint.Substring(0, endpoint.Length - 1);
        if (!BuilderUtil.IsValidHostnameOrIPAddress(endpoint))
            throw new InvalidEndpointException(string.Format("{0} is invalid hostname.", endpoint), "endpoint");
        string conn_url;
        if (endpoint.StartsWith("http"))
            throw new InvalidEndpointException(
                string.Format("{0} the value of the endpoint has the scheme (http/https) in it.", endpoint),
                "endpoint");
        var enable_https = Environment.GetEnvironmentVariable("ENABLE_HTTPS");
        var scheme = enable_https != null && enable_https.Equals("1") ? "https://" : "http://";
        conn_url = scheme + endpoint;
        var hostnameOfUri = string.Empty;
        Uri url = null;
        url = new Uri(conn_url);
        hostnameOfUri = url.Authority;
        if (!string.IsNullOrEmpty(hostnameOfUri) && !BuilderUtil.IsValidHostnameOrIPAddress(hostnameOfUri))
            throw new InvalidEndpointException(string.Format("{0}, {1} is invalid hostname.", endpoint, hostnameOfUri),
                "endpoint");

        return url;
    }

    public MinioClient WithRegion()
    {
        // Set region to its default value if empty or null
        Region = "us-east-1";
        return this;
    }
}