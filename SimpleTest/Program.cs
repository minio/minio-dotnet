/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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
            var minio = new MinioClient(ConfigurationManager.AppSettings["Endpoint"],
                                             ConfigurationManager.AppSettings["AccessKey"],
                                             ConfigurationManager.AppSettings["SecretKey"]).WithSSL();

            var getListBucketsTask = minio.ListBucketsAsync();
            try
            {
                Task.WaitAll(getListBucketsTask); // block while the task completes
            }
            catch (AggregateException aggEx)
            {
                aggEx.Handle(HandleBatchExceptions);
            }
            var list = getListBucketsTask.Result;

            foreach (Bucket bucket in list.Buckets)
            {
                Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
            }

            //Supply a new bucket name
            Task.WaitAll(minio.MakeBucketAsync("mynewbucket"));

            var bucketExistTask = minio.BucketExistsAsync("mynewbucket");
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
                Console.Out.WriteLine("Handling the ArgumentNullException.");
                //I handled this Exception, return true.
                return true;
            }
            else
            {
                //I'm only handling ArgumentNullExceptions.
                Console.Out.WriteLine(string.Format("I'm not handling the {0}.", exceptionToHandle.GetType()));
                //I didn't handle this Exception, return false.
                return false;
            }
        }

    }
}
