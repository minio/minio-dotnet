using System;

using Minio;
using Minio.DataModel;

using System.Threading.Tasks;

namespace SimpleTest
{
    class Program
    {
        static void Main(string[] args)
        { 

            var minio = new Minio.MinioRestClient(endpoint:"play.minio.io:9000",
                accessKey:"Q3AM3UQ867SPQQA43P2F",
                secretKey:"zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                ).WithSSL();
           

            var getListBucketsTask = minio.Buckets.ListBucketsAsync();
            try
            {
                Task.WaitAll(getListBucketsTask); // block while the task completes
            } catch(AggregateException aggEx)
            {
                aggEx.Handle(HandleBatchExceptions);
            }
            var list = getListBucketsTask.Result;

            foreach (Bucket bucket in list.Buckets)
            {
                Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
            }
          
            Task.WaitAll(minio.Buckets.MakeBucketAsync("bucket2"));

            var bucketExistTask = minio.Buckets.BucketExistsAsync("bucket2");
            Task.WaitAll(bucketExistTask);
            var found = bucketExistTask.Result;
            Console.Out.WriteLine("bucket was " + found);
            Console.ReadLine();
        }
        private static bool HandleBatchExceptions(Exception exceptionToHandle)
      {
         if (exceptionToHandle is ArgumentNullException)
          {
               //I'm handling the ArgumentNullException.
               Console.WriteLine("Handling the ArgumentNullException.");
               //I handled this Exception, return true.
               return true;
          }
        else
         {
              //I'm only handling ArgumentNullExceptions.
             Console.WriteLine(string.Format("I'm not handling the {0}.", exceptionToHandle.GetType()));
              //I didn't handle this Exception, return false.
              return false;
         }          
    }

    }
}
