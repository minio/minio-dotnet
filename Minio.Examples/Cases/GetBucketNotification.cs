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
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio.Examples.Cases
{
    class GetBucketNotification
    {
        // Get bucket notifications - this works only with AWS endpoint
        public async static Task Run(Minio.MinioClient minio, 
                                     string bucketName = "my-bucket-name")
        {
            try
            {
                Console.Out.WriteLine("Running example for API: GetBucketNotificationsAsync");
                BucketNotification notifications = await minio.GetBucketNotificationsAsync(bucketName);
                Console.Out.WriteLine("Notifications is " + notifications.ToXML() + " for bucket " + bucketName);
                Console.Out.WriteLine();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Error parsing bucket notifications - make sure that you are running this call against AWS end point");
            }
        }
    }
}
