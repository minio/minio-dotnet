namespace Minio
{
    using Android.Util;

    internal class LogProviderImpl : LogProvider
    {
        public override void Trace(string message)
        {
            Log.Debug("MINIO", message);
        }
    }
}