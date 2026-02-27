using Minio.Helpers;
using Minio.Model;
using Minio.Model.Notification;

namespace Minio;

/// <summary>
/// Callback delegate invoked periodically to report upload or download progress.
/// </summary>
/// <param name="position">The number of bytes transferred so far.</param>
/// <param name="length">The total number of bytes to transfer, or <c>-1</c> if unknown.</param>
public delegate void ProgressHandler(long position, long length);

/// <summary>
/// Defines the main interface for interacting with a MinIO or S3-compatible object storage service.
/// Covers bucket management, object operations, notifications, object locking, and versioning.
/// </summary>
public interface IMinioClient
{
    // Bucket operations

    /// <summary>
    /// Creates a new bucket with the specified name and optional settings.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to create.</param>
    /// <param name="objectLocking">
    /// When <see langword="true"/>, enables S3 Object Lock on the new bucket.
    /// Object Lock must be enabled at bucket creation time and cannot be added later.
    /// </param>
    /// <param name="region">
    /// The AWS region in which to create the bucket. Defaults to the region configured
    /// on the client when left empty.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that completes with the location of the newly created bucket.</returns>
    Task<string> CreateBucketAsync(string bucketName, bool objectLocking = false, string region = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified bucket. The bucket must be empty before it can be deleted.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteBucketAsync(string bucketName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a bucket with the specified name exists and is accessible.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to check.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that completes with <see langword="true"/> if the bucket exists and is accessible;
    /// otherwise <see langword="false"/>.
    /// </returns>
    Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all buckets accessible to the current credentials.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>An async sequence of <see cref="BucketInfo"/> items, one per bucket.</returns>
    IAsyncEnumerable<BucketInfo> ListBucketsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the tags currently associated with the specified bucket.
    /// </summary>
    /// <param name="bucketName">The name of the bucket whose tags to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that completes with a dictionary of tag key-value pairs,
    /// or <see langword="null"/> if no tags are set on the bucket.
    /// </returns>
    public Task<IDictionary<string, string>?> GetBucketTaggingAsync(string bucketName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or replaces the tags on the specified bucket.
    /// Pass <see langword="null"/> or an empty collection to clear all tags.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to tag.</param>
    /// <param name="tags">
    /// A collection of key-value tag pairs to apply to the bucket,
    /// or <see langword="null"/> to remove all existing tags.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous set-tagging operation.</returns>
    public Task SetBucketTaggingAsync(string bucketName, IEnumerable<KeyValuePair<string, string>>? tags, CancellationToken cancellationToken = default);

    // Object operations

    /// <summary>
    /// Initiates a new multipart upload session for the specified object and returns an upload ID
    /// that must be supplied to subsequent <see cref="UploadPartAsync"/> and
    /// <see cref="CompleteMultipartUploadAsync"/> calls.
    /// </summary>
    /// <param name="bucketName">The name of the bucket that will hold the object.</param>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="options">Optional settings such as content type, metadata, and storage class.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that completes with the result containing the upload ID for this session.</returns>
    Task<CreateMultipartUploadResult> CreateMultipartUploadAsync(string bucketName, string key, CreateMultipartUploadOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a single part within an existing multipart upload session.
    /// Each part (except the last) must be at least 5 MB in size.
    /// </summary>
    /// <param name="bucketName">The name of the bucket containing the in-progress upload.</param>
    /// <param name="key">The object key of the in-progress upload.</param>
    /// <param name="uploadId">The upload ID returned by <see cref="CreateMultipartUploadAsync"/>.</param>
    /// <param name="partNumber">The 1-based part number (1–10000).</param>
    /// <param name="stream">The data stream for this part.</param>
    /// <param name="options">Optional settings such as checksums for this part.</param>
    /// <param name="progress">Optional callback invoked as data is uploaded.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that completes with the result containing the ETag for the uploaded part.</returns>
    Task<UploadPartResult> UploadPartAsync(string bucketName, string key, string uploadId, int partNumber, Stream stream, UploadPartOptions? options = null, ProgressHandler? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a multipart upload by assembling the previously uploaded parts into a final object.
    /// </summary>
    /// <param name="bucketName">The name of the bucket containing the in-progress upload.</param>
    /// <param name="key">The object key of the in-progress upload.</param>
    /// <param name="uploadId">The upload ID returned by <see cref="CreateMultipartUploadAsync"/>.</param>
    /// <param name="parts">
    /// An ordered collection of part identifiers (part number and ETag) returned by
    /// <see cref="UploadPartAsync"/>.
    /// </param>
    /// <param name="options">Optional settings such as checksums for the completed object.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that completes with the result containing the final object's ETag and location.</returns>
    Task<CompleteMultipartUploadResult> CompleteMultipartUploadAsync(string bucketName, string key, string uploadId, IEnumerable<PartInfo> parts, CompleteMultipartUploadOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aborts a multipart upload session and discards all parts that were uploaded.
    /// </summary>
    /// <param name="bucketName">The name of the bucket containing the in-progress upload.</param>
    /// <param name="key">The object key of the in-progress upload.</param>
    /// <param name="uploadId">The upload ID returned by <see cref="CreateMultipartUploadAsync"/>.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous abort operation.</returns>
    Task AbortMultipartUploadAsync(string bucketName, string key, string uploadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads an object to the specified bucket using a single PUT request.
    /// For large objects, prefer the multipart upload APIs.
    /// </summary>
    /// <param name="bucketName">The name of the destination bucket.</param>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="stream">The data stream to upload as the object body.</param>
    /// <param name="options">Optional settings such as content type, metadata, and storage class.</param>
    /// <param name="progress">Optional callback invoked as data is uploaded.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous put operation.</returns>
    Task PutObjectAsync(string bucketName, string key, Stream stream, PutObjectOptions? options = null, ProgressHandler? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves metadata for an object without downloading its content (HTTP HEAD request).
    /// </summary>
    /// <param name="bucketName">The name of the bucket containing the object.</param>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="options">Optional settings such as version ID or conditional headers.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that completes with an <see cref="ObjectInfo"/> containing the object's metadata.</returns>
    Task<ObjectInfo> HeadObjectAsync(string bucketName, string key, GetObjectOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads an object from the specified bucket, returning its content stream and metadata.
    /// The caller is responsible for disposing the returned stream.
    /// </summary>
    /// <param name="bucketName">The name of the bucket containing the object.</param>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="options">Optional settings such as version ID, byte range, or conditional headers.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that completes with a <see cref="ObjectInfoStream"/> that holds both the object's content
    /// and an <see cref="ObjectInfo"/> describing the object metadata.
    /// </returns>
    Task<ObjectInfoStream> GetObjectAsync(string bucketName, string key, GetObjectOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a single object (or a specific version of an object) from the specified bucket.
    /// </summary>
    /// <param name="bucketName">The name of the bucket containing the object.</param>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="versionId">The specific version ID to delete, or <see langword="null"/> to delete the current version.</param>
    /// <param name="bypassGovernanceRetention">
    /// When <see langword="true"/>, bypasses Governance-mode object retention restrictions.
    /// Requires the <c>s3:BypassGovernanceRetention</c> permission.
    /// </param>
    /// <param name="expectedBucketOwner">
    /// The AWS account ID of the expected bucket owner. The request fails if the actual owner differs.
    /// </param>
    /// <param name="mfa">The MFA device serial number and token code, if MFA delete is enabled on the bucket.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteObjectAsync(string bucketName, string key, string? versionId = null, bool bypassGovernanceRetention = false, string? expectedBucketOwner = null, string? mfa = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple objects from the specified bucket in a single request.
    /// Errors for individual objects are silently ignored; use <see cref="DeleteObjectsVerboseAsync"/>
    /// to receive per-object results.
    /// </summary>
    /// <param name="bucketName">The name of the bucket containing the objects.</param>
    /// <param name="objects">A collection of object identifiers (key and optional version ID) to delete.</param>
    /// <param name="bypassGovernanceRetention">
    /// When <see langword="true"/>, bypasses Governance-mode object retention restrictions.
    /// </param>
    /// <param name="expectedBucketOwner">
    /// The AWS account ID of the expected bucket owner. The request fails if the actual owner differs.
    /// </param>
    /// <param name="mfa">The MFA device serial number and token code, if MFA delete is enabled on the bucket.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous batch delete operation.</returns>
    Task DeleteObjectsAsync(string bucketName, IEnumerable<ObjectIdentifier> objects, bool bypassGovernanceRetention = false, string? expectedBucketOwner = null, string? mfa = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple objects from the specified bucket in a single request and streams
    /// per-object success or error results back to the caller.
    /// </summary>
    /// <param name="bucketName">The name of the bucket containing the objects.</param>
    /// <param name="objects">A collection of object identifiers (key and optional version ID) to delete.</param>
    /// <param name="bypassGovernanceRetention">
    /// When <see langword="true"/>, bypasses Governance-mode object retention restrictions.
    /// </param>
    /// <param name="expectedBucketOwner">
    /// The AWS account ID of the expected bucket owner. The request fails if the actual owner differs.
    /// </param>
    /// <param name="mfa">The MFA device serial number and token code, if MFA delete is enabled on the bucket.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>An async sequence of <see cref="DeleteResult"/> items indicating success or failure for each object.</returns>
    IAsyncEnumerable<DeleteResult> DeleteObjectsVerboseAsync(string bucketName, IEnumerable<ObjectIdentifier> objects, bool bypassGovernanceRetention = false, string? expectedBucketOwner = null, string? mfa = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists objects in the specified bucket, optionally filtered and paginated.
    /// Uses the S3 ListObjectsV2 API and streams results as an async sequence.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to list objects in.</param>
    /// <param name="continuationToken">
    /// A token returned by a previous call that resumes listing from where it left off.
    /// </param>
    /// <param name="delimiter">
    /// A character used to group keys. Keys that contain the delimiter after the prefix are
    /// grouped into a common prefix and not listed individually.
    /// </param>
    /// <param name="includeMetadata">
    /// When <see langword="true"/>, requests per-object metadata such as content type and user metadata
    /// (MinIO-specific extension).
    /// </param>
    /// <param name="fetchOwner">
    /// When set, requests the owner information for each object. Pass <c>"true"</c> to enable.
    /// </param>
    /// <param name="pageSize">Maximum number of objects to return per API page. Use <c>0</c> for the server default.</param>
    /// <param name="prefix">Limits the response to keys that begin with the specified prefix.</param>
    /// <param name="startAfter">Instructs the server to start listing after this key.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>An async sequence of <see cref="ObjectItem"/> items, one per listed object.</returns>
    IAsyncEnumerable<ObjectItem> ListObjectsAsync(string bucketName, string? continuationToken = null, string? delimiter = null, bool includeMetadata = false, string? fetchOwner = null, int pageSize = 0, string? prefix = null, string? startAfter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the parts that have been uploaded for an in-progress multipart upload.
    /// </summary>
    /// <param name="bucketName">The name of the bucket containing the in-progress upload.</param>
    /// <param name="key">The object key of the in-progress upload.</param>
    /// <param name="uploadId">The upload ID of the multipart upload session.</param>
    /// <param name="pageSize">Maximum number of parts to return per API page. Use <c>0</c> for the server default.</param>
    /// <param name="partNumberMarker">
    /// The part number after which listing should begin. Parts with a number less than or equal
    /// to this value are not returned.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>An async sequence of <see cref="PartItem"/> items describing each uploaded part.</returns>
    IAsyncEnumerable<PartItem> ListPartsAsync(string bucketName, string key, string uploadId, int pageSize = 0, string? partNumberMarker = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all in-progress multipart uploads for the specified bucket.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to query for in-progress uploads.</param>
    /// <param name="delimiter">A character used to group keys; matched prefixes are collapsed.</param>
    /// <param name="encodingType">Specifies how the keys in the response should be encoded (e.g., <c>url</c>).</param>
    /// <param name="keyMarker">The key after which listing should begin.</param>
    /// <param name="pageSize">Maximum number of uploads to return per API page. Use <c>0</c> for the server default.</param>
    /// <param name="prefix">Limits results to uploads for keys that begin with the specified prefix.</param>
    /// <param name="uploadIdMarker">
    /// Together with <paramref name="keyMarker"/>, specifies the upload after which listing should begin.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>An async sequence of <see cref="UploadItem"/> items describing each in-progress upload.</returns>
    IAsyncEnumerable<UploadItem> ListMultipartUploadsAsync(string bucketName, string? delimiter = null, string? encodingType = null, string? keyMarker = null, int pageSize = 0, string? prefix = null, string? uploadIdMarker = null, CancellationToken cancellationToken = default);

    // Bucket notifications

    /// <summary>
    /// Retrieves the current notification configuration for the specified bucket.
    /// </summary>
    /// <param name="bucketName">The name of the bucket whose notification configuration to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that completes with the <see cref="BucketNotification"/> configuration.</returns>
    Task<BucketNotification> GetBucketNotificationsAsync(string bucketName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or replaces the notification configuration for the specified bucket.
    /// </summary>
    /// <param name="bucketName">The name of the bucket on which to set notifications.</param>
    /// <param name="bucketNotification">The notification configuration to apply.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous set-notifications operation.</returns>
    Task SetBucketNotificationsAsync(string bucketName, BucketNotification bucketNotification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to real-time bucket notifications for the specified event types using
    /// the MinIO listen-bucket-notifications endpoint, returning an observable event stream.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to listen to.</param>
    /// <param name="events">The event types to subscribe to (e.g., put, delete, replicate).</param>
    /// <param name="prefix">Limits notifications to objects whose keys start with this prefix.</param>
    /// <param name="suffix">Limits notifications to objects whose keys end with this suffix.</param>
    /// <param name="cancellationToken">A token to cancel the long-running listen operation.</param>
    /// <returns>
    /// A task that completes with an <see cref="IObservable{T}"/> of <see cref="NotificationEvent"/>
    /// items emitted as events occur on the bucket.
    /// </returns>
    Task<IObservable<NotificationEvent>> ListenBucketNotificationsAsync(string bucketName, IEnumerable<EventType> events, string prefix = "", string suffix = "", CancellationToken cancellationToken = default);

    // Object locking

    /// <summary>
    /// Retrieves the Object Lock configuration for the specified bucket.
    /// Object Lock must have been enabled when the bucket was created.
    /// </summary>
    /// <param name="bucketName">The name of the bucket whose Object Lock configuration to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that completes with the <see cref="ObjectLockConfiguration"/> for the bucket.</returns>
    Task<ObjectLockConfiguration> GetObjectLockConfigurationAsync(string bucketName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates the default Object Lock retention rule for the specified bucket.
    /// Pass <see langword="null"/> to remove the default retention rule.
    /// </summary>
    /// <param name="bucketName">The name of the bucket on which to set the Object Lock configuration.</param>
    /// <param name="defaultRetentionRule">
    /// The default retention rule (mode and period) to apply to new objects,
    /// or <see langword="null"/> to remove the existing default rule.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous set-lock-configuration operation.</returns>
    Task SetObjectLockConfigurationAsync(string bucketName, RetentionRule? defaultRetentionRule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the versioning configuration for the specified bucket.
    /// </summary>
    /// <param name="bucketName">The name of the bucket whose versioning configuration to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that completes with the <see cref="VersioningConfiguration"/> for the bucket.</returns>
    Task<VersioningConfiguration> GetBucketVersioningAsync(string bucketName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the versioning state for the specified bucket.
    /// </summary>
    /// <param name="bucketName">The name of the bucket on which to configure versioning.</param>
    /// <param name="status">The desired versioning status (<c>Enabled</c> or <c>Suspended</c>).</param>
    /// <param name="mfaDelete">
    /// When <see langword="true"/>, enables MFA Delete, requiring MFA authentication for
    /// permanent object deletion or versioning state changes.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous set-versioning operation.</returns>
    Task SetBucketVersioningAsync(string bucketName, VersioningStatus status, bool mfaDelete = false, CancellationToken cancellationToken = default);

    // TODO: Add following bucket operations
    // SetBucketEncryptionAsync
    // GetBucketEncryptionAsync
    // RemoveBucketEncryptionAsync
    // SetBucketLifecycleAsync
    // GetBucketLifecycleAsync
    // RemoveBucketLifecycleAsync
    // GetBucketReplicationAsync
    // SetBucketReplicationAsync
    // RemoveBucketReplicationAsync
    // GetPolicyAsync
    // RemovePolicyAsync
    // SetPolicyAsync

    // TODO: Add following object operations
    // GetObjectLegalHoldAsync
    // SetObjectLegalHoldAsync
    // SetObjectRetentionAsync
    // GetObjectRetentionAsync
    // ClearObjectRetentionAsync
    // CopyObjectAsync
    // SelectObjectContentAsync
    // PresignedGetObjectAsync
    // PresignedPostPolicyAsync
    // PresignedPutObjectAsync
    // GetObjectTagsAsync
    // SetObjectTagsAsync
    // RemoveObjectTagsAsync
}
