namespace Minio.Helper;

/// <summary>
/// SyncProgress is a thread-free alternative to <see cref="System.Progress&lt;T&gt;"/>
/// and should be used if progress needs be determined in real-time.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>
/// The upload process uses asynchronous tasks to perform the upload,
/// so these events may be invoked on an arbitrary thread. Use the
/// regular <see cref="System.Progress&lt;T&gt;"/> if you need progress
/// to be reported on a fixed thread or provide your own thread
/// synchronization. Doing synchronous calls to other threads from
/// within this handler will degrade your upload performance.
/// </remarks>
public class SyncProgress<T> : IProgress<T>
{
    private readonly Action<T> handler;

    public SyncProgress(Action<T> handler)
    {
        this.handler = handler;
    }
    
    public void Report(T value)
    {
        handler(value);
    }
}
