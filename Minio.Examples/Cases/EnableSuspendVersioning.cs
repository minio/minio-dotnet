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
    class EnableSuspendVersioning
    {
        // Enable Versioning on a bucket
        public async static Task Run(MinioClient minio,
                                     string bucketName = "my-bucket-name",
                                     string region="us-east-1")
        {
            try
            {
                Console.WriteLine("Running example for API: EnableSuspendVersioning, ");
                var setArgs = new SetVersioningArgs()
                                        .WithBucket(bucketName)
                                        .WithRegion(region)
                                        .WithSSL(minio.IsSecure())
                                        .WithVersioningEnabled();
                var getArgs = new GetVersioningInfoArgs()
                                        .WithBucket(bucketName)
                                        .WithRegion(region)
                                        .WithSSL(minio.IsSecure());

                await minio.SetVersioningAsync(setArgs);
                VersioningConfiguration vc = await minio.GetVersioningInfoAsync(getArgs);
                Console.WriteLine("Versioning " + (vc != null && vc.IsVersioningEnabled()? "has been ":"has NOT been ") + "enabled for " + bucketName + ".");
                Console.WriteLine();

                setArgs = setArgs.WithVersioningSuspended();
                if ( vc != null )
                {
                    await minio.SetVersioningAsync(setArgs);
                    vc = await minio.GetVersioningInfoAsync(getArgs);
                    Console.WriteLine("Versioning " + (vc != null && vc.IsVersioningSuspended()? "has been ":"has NOT been ") + "suspended for " + bucketName + ".");
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