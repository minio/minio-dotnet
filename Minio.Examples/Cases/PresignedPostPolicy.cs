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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    public class PresignedPostPolicy
    {
        public async static Task Run(MinioClient client)
        {
            try
            {
                PostPolicy form = new PostPolicy();
                DateTime expiration = DateTime.UtcNow;
                form.SetExpires(expiration.AddDays(10));
                form.SetKey("my-objectname");
                form.SetBucket("my-bucketname");

                Tuple<string, Dictionary<string, string>> tuple = await client.PresignedPostPolicyAsync(form);
                string curlCommand = "curl -X POST ";
                foreach (KeyValuePair<string, string> pair in tuple.Item2)
                {
                    curlCommand = curlCommand + String.Format(" -F {0}={1}", pair.Key, pair.Value);
                }
                curlCommand = curlCommand + " -F file=@/etc/bashrc " + tuple.Item1; // https://s3.amazonaws.com/my-bucketname";
                Console.Out.WriteLine(curlCommand);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Exception ", e.Message);
            }
        }
    }
}
