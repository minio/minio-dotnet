using System.Reactive.Linq;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace Minio.ApiEndpoints;

public static class BucketOperationsExtensions
{
    // Used for backwards compatibility
    [Obsolete("Use the IAsyncEnumerable version instead")]
    public static IObservable<Item> ListObjectsAsync(this IBucketOperations self, ListObjectsArgs args)
    {
        return Observable.Create<Item>(async (obs, ct) =>
        {
            try
            {
                await foreach (var item in self.ListObjectsAsync(args, ct).ConfigureAwait(false))
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
