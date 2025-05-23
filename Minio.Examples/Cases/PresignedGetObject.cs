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

using Minio.DataModel.Args;

namespace Minio.Examples.Cases;

public static class PresignedGetObject
{
    public static async Task Run(IMinioClient client,
        string bucketName = "my-bucket-name",
        string objectName = "my-object-name")
    {
        if (client is null) throw new ArgumentNullException(nameof(client));

        var reqParams = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "response-content-type", "application/json" }
        };
        var args = new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithExpiry(1000)
            .WithHeaders(reqParams);
        _ = await client.PresignedGetObjectAsync(args).ConfigureAwait(false);
    }
}
