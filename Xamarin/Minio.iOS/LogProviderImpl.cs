namespace Minio
{
    using System;

    internal class LogProviderImpl : LogProvider
    {
        public override void Trace(string message)
        {
            Console.WriteLine(message);
        }
    }
}