using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Minio.Exceptions;

namespace Minio;

public class AzureBlobClient : MinioClient, IMinioClient
{
    private readonly string connectionString;
    private const string MetaElementPrefix = "X-Amz-Meta-";

    public AzureBlobClient(string _connectionString)
    {
        connectionString = _connectionString;
    }


    public new async Task<bool> BucketExistsAsync(BucketExistsArgs args,
        CancellationToken cancellationToken = default)
    {

        args?.Validate();

        var blobServiceClient = new BlobServiceClient(connectionString);
        var container = blobServiceClient.GetBlobContainerClient(args.BucketName);

        return await container.ExistsAsync(cancellationToken).ConfigureAwait(false);

    }


    public new async IAsyncEnumerable<Item> ListObjectsEnumAsync(ListObjectsArgs args,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (args == null) throw new ArgumentNullException(nameof(args));

        args.Validate();

        if (!await BucketExistsAsync(new BucketExistsArgs().WithBucket(args.BucketName), cancellationToken)
                .ConfigureAwait(false))
            throw new BucketNotFoundException(args.BucketName, $"Bucket \"{args.BucketName}\" is not found");

        var containerClient = new BlobContainerClient(connectionString, args.BucketName);

        if (args.Recursive)
        {
            // Flat listing (like MinIO recursive = true)
            var blobList = containerClient.GetBlobsAsync(prefix: args.Prefix, states: BlobStates.Version, cancellationToken: cancellationToken).ConfigureAwait(false);

            var orderedVersions = new List<BlobItem>();
            await foreach (var version in blobList)
                orderedVersions.Add(version);

            var sorted = orderedVersions.OrderByDescending(v => v.Properties.CreatedOn);

            foreach (var blobItem in sorted)
            {
                yield return new Item
                {
                    Key = blobItem.Name,
                    LastModified = blobItem.Properties.LastModified?.UtcDateTime.ToString(CultureInfo.InvariantCulture) ?? default,
                    ETag = blobItem.Properties.ETag?.ToString(),
                    Size = (ulong)(blobItem.Properties.ContentLength ?? 0),
                    VersionId = blobItem.VersionId,
                    ContentType = blobItem.Properties.ContentType,
                    Expires = blobItem.Properties.ExpiresOn?.UtcDateTime.ToString(CultureInfo.InvariantCulture) ?? default,
                    UserMetadata = blobItem.Metadata,
                    IsDir = false,
                    IsLatest = blobItem.IsLatestVersion == true
                };
            }
        }
        else
        {
            // Hierarchical listing (like MinIO recursive = false, delimiter = "/")
            var blobList = containerClient.GetBlobsByHierarchyAsync(states: BlobStates.Version, delimiter: "/", prefix: args.Prefix, cancellationToken: cancellationToken).ConfigureAwait(false);

            var orderedVersions = new List<BlobHierarchyItem>();
            await foreach (var version in blobList)
                orderedVersions.Add(version);

            var sorted = orderedVersions.OrderByDescending(v => v.Blob.Properties.CreatedOn);

            foreach (var item in sorted)
            {
                if (item.IsPrefix)
                {
                    yield return new Item
                    {
                        Key = item.Prefix,
                        IsDir = true
                    };
                }
                else
                {
                    yield return new Item
                    {
                        Key = item.Blob.Name,
                        LastModified = item.Blob.Properties.LastModified?.UtcDateTime.ToString(CultureInfo.InvariantCulture) ?? default,
                        ETag = item.Blob.Properties.ETag?.ToString(),
                        Size = (ulong)(item.Blob.Properties.ContentLength ?? 0),
                        VersionId = item.Blob.VersionId,
                        ContentType = item.Blob.Properties.ContentType,
                        Expires = item.Blob.Properties.ExpiresOn?.UtcDateTime.ToString(CultureInfo.InvariantCulture) ?? default,
                        UserMetadata = item.Blob.Metadata,
                        IsDir = false,
                        IsLatest = item.Blob.IsLatestVersion == true
                    };
                }
            }
        }
    }


    public new Task<string> PresignedGetObjectAsync(PresignedGetObjectArgs args)
    {
        args?.Validate();

        //removing "/" in case of "request" as the inio prefix is saved as "/request" in the DB and throws error when trying to generate the SAS
        var blobClient = new BlobClient(connectionString, args.BucketName, args.ObjectName.StartsWith("/", StringComparison.OrdinalIgnoreCase) ? args.ObjectName[1..] : args.ObjectName);
        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddSeconds(args.Expiry));

        return Task.FromResult(sasUri.ToString());
    }


    public new async Task<PutObjectResponse> PutObjectAsync(PutObjectArgs args,
        CancellationToken cancellationToken = default)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        var container = blobServiceClient.GetBlobContainerClient(args.BucketName);

        if (!container.Exists())
        {
            _ = await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var blobClient = container.GetBlobClient(args.ObjectName);

        var userMetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var xHeader in args.Headers.Where(x =>
                     x.Key.StartsWith(MetaElementPrefix,
                         StringComparison.OrdinalIgnoreCase)))
        {
            var key = xHeader.Key[MetaElementPrefix.Length..];
            userMetaData[key] = xHeader.Value;
        }
        var response = await blobClient.UploadAsync(content: args.ObjectStreamData, new BlobHttpHeaders { ContentType = args.ContentType }, metadata: userMetaData).ConfigureAwait(false);

        return new PutObjectResponse(
            (HttpStatusCode)response.GetRawResponse().Status,
            Encoding.Default.GetString(response.GetRawResponse().Content),
            response.GetRawResponse().Headers.ToDictionary(h => h.Name, h => h.Value, StringComparer.OrdinalIgnoreCase),
            args.ObjectSize,
            args.ObjectName
            );
    }


    public new async Task RemoveObjectAsync(RemoveObjectArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();

        var blobClient = new BlobClient(connectionString, args.BucketName, args.ObjectName);

        if (!string.IsNullOrEmpty(args.VersionId))
        {
            //to remove a version that is the promoted one, another version must be promoted otherwise it will throw 403 error
            var latestBlobItem = GetDifferentLatestVersionIfExists(args.BucketName, args.ObjectName, args.VersionId);
            if (latestBlobItem != null)
            {
                await PromoteLatestVersionAsync(latestBlobItem.VersionId, blobClient).ConfigureAwait(false);
                blobClient = blobClient.WithVersion(args.VersionId);
            }
        }

        _ = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }


    public new async Task<ObjectStat> StatObjectAsync(StatObjectArgs args, CancellationToken cancellationToken = default)
    {
        if (!await BucketExistsAsync(new BucketExistsArgs().WithBucket(args.BucketName), cancellationToken)
                .ConfigureAwait(false))
            throw new BucketNotFoundException(args.BucketName, $"Bucket \"{args.BucketName}\" is not found");

        args?.Validate();

        var blobClient = new BlobClient(connectionString, args.BucketName, args.ObjectName);

        if (!await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
            return new StatObjectResponse(HttpStatusCode.NotFound, null, new Dictionary<string, string>(StringComparer.Ordinal), args).ObjectInfo;

        BlobProperties properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var responseHeaders = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "content-length", properties.ContentLength.ToString(CultureInfo.InvariantCulture) },
                { "etag", properties.ETag.ToString() },
                { "last-modified", properties.LastModified.UtcDateTime.ToString(CultureInfo.InvariantCulture) },
                { "content-type", properties.ContentType },
                { "x-amz-version-id", properties.VersionId }
            };

        foreach (var metaData in properties.Metadata)
            responseHeaders.Add($"{MetaElementPrefix}{metaData.Key}", metaData.Value);

        var statResponse = new StatObjectResponse(HttpStatusCode.OK, null, responseHeaders, args);

        return statResponse.ObjectInfo;
    }


    private BlobItem GetDifferentLatestVersionIfExists(string bucketName, string objectName, string versionToDelete)
    {
        var containerClient = new BlobContainerClient(connectionString, bucketName);

        // List all versions of the blob
        var versions = containerClient
            .GetBlobs(BlobTraits.None, BlobStates.Version, prefix: objectName);

        BlobItem latestVersion = null;

        foreach (var v in versions)
        {
            // Skip the one we want to delete
            if (string.Equals(v.VersionId, versionToDelete, StringComparison.Ordinal))
                continue;

            // Pick the most recent version by LastModified
            if (latestVersion == null || v.Properties.LastModified > latestVersion.Properties.LastModified)
            {
                latestVersion = v;
            }
        }

        return latestVersion;
    }


    private Task PromoteLatestVersionAsync(string latestVersion, BlobClient blobClient)
    {
        var blobClientForPromotion = blobClient;
        var versionClient = blobClientForPromotion.WithVersion(latestVersion);

        // Promote the latest version (overwrite the current blob with it)
        _ = blobClientForPromotion.StartCopyFromUriAsync(versionClient.Uri);
        // Delete the version which has been promoted otherwise it will show two identical versions
        _ = versionClient.DeleteAsync();

        return Task.CompletedTask;
    }
}
