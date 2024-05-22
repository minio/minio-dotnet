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
        return Observable.Create<Item>(async (obs, ct) =>
        {
            try
            {
                await foreach (var item in self.ListObjectsEnumAsync(args, ct).ConfigureAwait(false))
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
    [Obsolete("Use the ListIncompleteUploadsEnumAsync instead")]
    public static IObservable<Upload> ListIncompleteUploads(this IObjectOperations self, ListIncompleteUploadsArgs args)
    {
        return Observable.Create<Upload>(async (obs, ct) =>
        {
            try
            {
                await foreach (var item in self.ListIncompleteUploadsEnumAsync(args, ct).ConfigureAwait(false))
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
