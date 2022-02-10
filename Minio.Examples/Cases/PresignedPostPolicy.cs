﻿/*
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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    public delegate PostPolicy DefaultPolicy(string bucketName,
                                             string objectName,
                                             DateTime expiration);
    public class PresignedPostPolicy
    {
        public async static Task Run(MinioClient client,
                                     string bucketName = "my-bucketname",
                                     string objectName = "my-objectname")
        {
            // default value for expiration is 2 minutes
            DateTime expiration = DateTime.UtcNow.AddMinutes(2);

            PostPolicy form = new PostPolicy();
            form.SetKey(objectName);
            form.SetBucket(bucketName);
            form.SetExpires(expiration);

            PresignedPostPolicyArgs args = new PresignedPostPolicyArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithPolicy(form);

            var tuple = await client.PresignedPostPolicyAsync(form);
            string curlCommand = "curl -k --insecure -X POST";
            foreach (KeyValuePair<string, string> pair in tuple.Item2)
            {
                curlCommand = curlCommand + $" -F {pair.Key}={pair.Value}";
            }
            curlCommand = curlCommand + " -F file=@/etc/issue " + tuple.Item1 + bucketName + "/";
        }
    }
}
