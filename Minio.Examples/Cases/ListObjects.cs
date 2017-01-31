using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio.DataModel;
namespace Minio.Examples.Cases
{
    class ListObjects
    {
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                var bucketName = "bucket-name";
                var prefix = "object-prefix";
                var recursive = false;
                bucketName = "mountshasta";
                prefix = null;
               /* IObservable<Item> observable = minio.Buckets.ListObjectsAsync(bucketName, prefix, recursive);

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
