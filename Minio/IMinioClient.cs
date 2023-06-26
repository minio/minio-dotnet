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

using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Encryption;
using Minio.DataModel.ILM;
using Minio.DataModel.Notification;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Replication;
using Minio.DataModel.Response;
using Minio.DataModel.Result;
using Minio.DataModel.Select;
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
    Task<(Uri, IDictionary<string, string>)> PresignedPostPolicyAsync(PostPolicy policy);
    Task<(Uri, IDictionary<string, string>)> PresignedPostPolicyAsync(PresignedPostPolicyArgs args);
    Task<string> PresignedPutObjectAsync(PresignedPutObjectArgs args);
    Task<PutObjectResponse> PutObjectAsync(PutObjectArgs args, CancellationToken cancellationToken = default);

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
    Task<HttpResponseMessage> WrapperGetAsync(Uri uri);
    Task WrapperPutAsync(Uri uri, StreamContent strm);
}