namespace Minio
{
    using System;

    public class LogProviderImpl : LogProvider
    {
        public override void Trace(string message)
        {
            Console.WriteLine(message);
        }
    }
}