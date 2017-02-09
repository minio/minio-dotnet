using System;
using Minio.DataModel;

namespace Minio.Examples.Cases
{
    
    class ListObjects
    {
        //List objects matching optional prefix in a specified bucket.
        public static void Run(Minio.MinioClient minio,
                                     string bucketName = "my-bucket-name",
                                     string prefix = null,
                                     bool recursive = false)
        {
            try
            {
                bucketName = "mountshasta";
                prefix = null;

               /* IObservable<Item> observable = minio.Buckets.ListObjectsAsync(bucketName, prefix, recursive);

                IObservable<Item> observable = minio.Api.ListObjectsAsync(bucketName, prefix, recursive);


                IDisposable subscription = observable.Subscribe(
                    item => Console.WriteLine("OnNext: {0}", item.Key),
                    ex => Console.WriteLine("OnError: {0}", ex.Message),
                    () => Console.WriteLine("OnComplete: {0}"));

                    */
                // subscription.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
