using System.Reactive.Linq;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace Minio.ApiEndpoints;

public static class CompatibilityExtensions
{
    // Used for backwards compatibility
    [Obsolete("Use the ListObjectsEnumAsync instead")]
    public static IObservable<Item> ListObjectsAsync(this IBucketOperations self, ListObjectsArgs args)
    {
        return self.InternalListObjectsAsync(args, CancellationToken.None);
    }

    // Used for backwards compatibility (with added warning)
    [Obsolete("Use the ListObjectsEnumAsync instead (also don't mix cancellation tokens and observables)")]
    public static IObservable<Item> ListObjectsAsync(this IBucketOperations self, ListObjectsArgs args,
        CancellationToken cancellationToken)
    {
        return self.InternalListObjectsAsync(args, cancellationToken);
    }

    private static IObservable<Item> InternalListObjectsAsync(this IBucketOperations self, ListObjectsArgs args,
        CancellationToken cancellationToken)
    {
        return Observable.Create<Item>(async (obs, ct) =>
        {
            try
            {
                using var ctEffective = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                await foreach (var item in self.ListObjectsEnumAsync(args, ctEffective.Token).ConfigureAwait(false))
                    obs.OnNext(item);
                obs.OnCompleted();
            }
            catch (Exception exc)
            {
                obs.OnError(exc);
            }
        });
    }

    // Used for backwards compatibility
    [Obsolete("Use the ListObjectsEnumAsync instead")]
    public static IObservable<Upload> ListIncompleteUploads(this IObjectOperations self, ListIncompleteUploadsArgs args)
    {
        return self.InternalListIncompleteUploads(args, CancellationToken.None);
    }

    // Used for backwards compatibility (with added warning)
    [Obsolete("Use the ListIncompleteUploads instead (also don't mix cancellation tokens and observables)")]
    public static IObservable<Upload> ListIncompleteUploads(this IObjectOperations self, ListIncompleteUploadsArgs args,
        CancellationToken cancellationToken)
    {
        return self.InternalListIncompleteUploads(args, cancellationToken);
    }

    private static IObservable<Upload> InternalListIncompleteUploads(this IObjectOperations self,
        ListIncompleteUploadsArgs args, CancellationToken cancellationToken = default)
    {
        return Observable.Create<Upload>(async (obs, ct) =>
        {
            try
            {
                using var ctEffective = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                await foreach (var item in self.ListIncompleteUploadsEnumAsync(args, ctEffective.Token)
                                   .ConfigureAwait(false))
                    obs.OnNext(item);
                obs.OnCompleted();
            }
            catch (Exception exc)
            {
                obs.OnError(exc);
            }
        });
    }
}
