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

namespace MinioCoreTest.Cases
{
    public class MakeBucket
    {
        //Make a bucket
        public async static Task Run(MinioCore2.MinioClient minio,
                                     string bucketName="my-bucket-name")
        {
            try
            {
                await minio.MakeBucketAsync(bucketName);
                Console.Out.WriteLine("bucket-name created successfully");
            } 
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }

        
    }
}
