using Minio.Model;
using Minio.Model.Notification;

namespace Minio;

/// <summary>
/// Convenience extension methods for <see cref="IMinioClient"/> that provide simplified
/// overloads for common operations.
/// </summary>
public static class MinioClientExtensions
{
    /// <summary>
    /// Removes all notification configurations from the specified bucket by replacing
    /// the current configuration with an empty one.
    /// </summary>
    /// <param name="client">The <see cref="IMinioClient"/> instance to act on.</param>
    /// <param name="bucketName">The name of the bucket whose notifications should be cleared.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous remove-notifications operation.</returns>
    public static Task RemoveAllBucketNotificationsAsync(this IMinioClient client, string bucketName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SetBucketNotificationsAsync(bucketName, new BucketNotification(), cancellationToken);
    }

    /// <summary>
    /// Removes all tags from the specified bucket by calling
    /// <see cref="IMinioClient.SetBucketTaggingAsync"/> with a <see langword="null"/> tag collection.
    /// </summary>
    /// <param name="client">The <see cref="IMinioClient"/> instance to act on.</param>
    /// <param name="bucketName">The name of the bucket whose tags should be removed.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous delete-tagging operation.</returns>
    public static Task DeleteBucketTaggingAsync(this IMinioClient client, string bucketName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SetBucketTaggingAsync(bucketName, null, cancellationToken);
    }

    /// <summary>
    /// Removes the default Object Lock retention rule from the specified bucket by calling
    /// <see cref="IMinioClient.SetObjectLockConfigurationAsync"/> with a <see langword="null"/> retention rule.
    /// </summary>
    /// <param name="client">The <see cref="IMinioClient"/> instance to act on.</param>
    /// <param name="bucketName">The name of the bucket whose Object Lock configuration should be cleared.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous remove-lock-configuration operation.</returns>
    public static Task RemoveObjectLockConfigurationAsync(this IMinioClient client, string bucketName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SetObjectLockConfigurationAsync(bucketName, null, cancellationToken);
    }

    /// <summary>
    /// Subscribes to real-time bucket notifications for a single event type.
    /// This is a convenience overload of
    /// <see cref="IMinioClient.ListenBucketNotificationsAsync(string, IEnumerable{EventType}, string, string, CancellationToken)"/>
    /// that accepts a single <see cref="EventType"/> instead of a collection.
    /// </summary>
    /// <param name="client">The <see cref="IMinioClient"/> instance to act on.</param>
    /// <param name="bucketName">The name of the bucket to listen to.</param>
    /// <param name="eventType">The single event type to subscribe to.</param>
    /// <param name="prefix">Limits notifications to objects whose keys start with this prefix.</param>
    /// <param name="suffix">Limits notifications to objects whose keys end with this suffix.</param>
    /// <param name="cancellationToken">A token to cancel the long-running listen operation.</param>
    /// <returns>
    /// A task that completes with an <see cref="IObservable{T}"/> of <see cref="NotificationEvent"/>
    /// items emitted as events occur on the bucket.
    /// </returns>
    public static Task<IObservable<NotificationEvent>> ListenBucketNotificationsAsync(this IMinioClient client, string bucketName, EventType eventType, string prefix = "", string suffix = "", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.ListenBucketNotificationsAsync(bucketName, [eventType], prefix, suffix, cancellationToken);
    }
}
