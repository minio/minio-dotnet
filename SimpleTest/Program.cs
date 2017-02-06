
using System;
using Minio;
using Minio.DataModel;
 
using System.Configuration;
using System.Threading.Tasks;

using System.Net;

namespace SimpleTest
{
    class Program
    {
        static void Main(string[] args)
        { 

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                    | SecurityProtocolType.Tls11
                                    | SecurityProtocolType.Tls12;


            /// Note: s3 AccessKey and SecretKey needs to be added in App.config file
            /// See instructions in README.md on running examples for more information.
            var minio = new MinioRestClient(ConfigurationManager.AppSettings["Endpoint"],
                                             ConfigurationManager.AppSettings["AccessKey"],
                                             ConfigurationManager.AppSettings["SecretKey"]).WithSSL();
           
            var getListBucketsTask = minio.Api.ListBucketsAsync();
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
 
            //Supply a new bucket name
            Task.WaitAll(minio.Api.MakeBucketAsync("MyNewBucket"));

            var bucketExistTask = minio.Api.BucketExistsAsync("MyNewBucket");
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
