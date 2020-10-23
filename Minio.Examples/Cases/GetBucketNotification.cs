/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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

using Minio.DataModel;
using System;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class GetBucketNotification
    {
        // Get bucket notifications - this works only with AWS endpoint
        public async static Task Run(MinioClient minio, 
                                     string bucketName = "my-bucket-name")
        {
            try
            {
                Console.WriteLine("Running example for API: GetBucketNotificationsAsync");
                GetBucketNotificationsArgs args = new GetBucketNotificationsArgs()
                                                                .WithBucket(bucketName);
                BucketNotification notifications = await minio.GetBucketNotificationsAsync(args);
                Console.WriteLine($"Notifications is {notifications.ToXML()} for bucket {bucketName}");
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error parsing bucket notifications - make sure that you are running this call against AWS end point: {e.Message}");
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
