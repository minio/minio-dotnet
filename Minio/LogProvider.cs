namespace Minio
{
    using System;

    public class LogProvider
    {
        public virtual void Trace(string message)
        {
#if NETSTANDARD1_6 || NET452
            Console.WriteLine(message);
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}