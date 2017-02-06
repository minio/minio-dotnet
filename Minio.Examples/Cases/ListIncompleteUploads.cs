using Minio.DataModel;
using System;


namespace Minio.Examples.Cases
{
    class ListIncompleteUploads
    {
        //List incomplete uploads on the bucket matching specified prefix
        public static void Run(Minio.MinioRestClient minio,
                               string bucketName = "my-bucket-name", 
                                   string prefix = "my-object-name",
                                  bool recursive = true)
        {
            try
            {
                IObservable<Upload> observable = minio.Api.ListIncompleteUploads(bucketName, prefix, recursive);

                IDisposable subscription = observable.Subscribe(
                    item => Console.WriteLine("OnNext: {0}", item.Key),
                    ex => Console.WriteLine("OnError: {0}", ex.Message),
                    () => Console.WriteLine("OnComplete: {0}"));

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
        }
    }
}
