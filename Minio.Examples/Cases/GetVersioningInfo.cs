/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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
    class GetVersioningInfo
    {
        // Check if Versioning is Enabled on a bucket
        public async static Task Run(MinioClient minio,
                                     string bucketName = "my-bucket-name")
        {
            try
            {
                Console.WriteLine("Running example for API: GetVersioningInfo, ");
                // VersioningConfiguration vc = await minio.GetVersioningInfoAsync(bucketName);
                VersioningConfiguration vc = await minio.GetVersioningInfoAsync();
                if ( vc == null )
                {
                    Console.WriteLine("Versioning Configuration not available for bucket " + bucketName);
                    Console.WriteLine();
                    return;
                }
                if ( vc.IsNotVersioned() )
                {
                    Console.WriteLine("Versioning Configuration shows that versioning hasn't been enabled before for bucket " + bucketName);
                    Console.WriteLine();
                } else if ( vc.IsVersioningEnabled() )
                {
                    Console.WriteLine("Versioning Configuration shows that versioning is enabled for bucket " + bucketName);
                    Console.WriteLine();
                } else if ( vc.IsVersioningSuspended() )
                {
                    Console.WriteLine("Versioning Configuration shows that versioning has been suspended  for bucket " + bucketName);
                    Console.WriteLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }
}
