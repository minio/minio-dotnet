/*
 * Minimal Object Storage Library, (C) 2015 Minio, Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Minio.Client;

namespace Minio.ClientTests.Examples
{
    class ExamplePutObject
    {
        static int Main(string[] args)
        {
            var client = MinioClient.GetClient("https://s3.amazonaws.com", "ACCESSKEY", "SECRETKEY");

            byte[] data = System.Text.Encoding.UTF8.GetBytes("hello world");
            client.PutObject("bucket", "smallobj", 11, "application/octet-stream", new MemoryStream(data));

            return 0;
        }
    }
}
