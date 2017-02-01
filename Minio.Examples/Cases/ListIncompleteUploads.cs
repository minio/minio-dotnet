using Minio.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class ListIncompleteUploads
    {
        public static void Run(Minio.MinioRestClient minio)
        {
            try
            {
                var bucketName = "bucket-name";
                var bucketObject = "bucket-object";

                bucketName = "mountshasta";
                bucketObject = "multi1112";
                IObservable<Upload> observable = minio.Api.ListIncompleteUploads(bucketName, bucketObject, true);

                IDisposable subscription = observable.Subscribe(
                    item => Console.WriteLine("OnNext: {0}", item.Key),
                    ex => Console.WriteLine("OnError: {0}", ex.Message),
                    () => Console.WriteLine("OnComplete: {0}"));

               // subscription.Dispose();

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
        }
    }
}
