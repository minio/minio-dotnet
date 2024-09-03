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

using Minio.DataModel.Args;

namespace Minio.Examples.Cases;

public static class GetPresignedUrl
{
    public static async Task Run(IMinioClient client,
        string bucketName = "my-bucket-name",
        string objectName = "my-object-name")
    {
        if (client is null) throw new ArgumentNullException(nameof(client));

        try
        {
            await GetPresignedUrlByRequest(client, 
                GetPresignedUrlArgs.PresignedUrlHttpMethod.Get, bucketName, objectName).ConfigureAwait(false);
            await GetPresignedUrlByRequest(client, 
                GetPresignedUrlArgs.PresignedUrlHttpMethod.Put, bucketName, objectName).ConfigureAwait(false);
            await GetPresignedUrlByRequest(client, 
                GetPresignedUrlArgs.PresignedUrlHttpMethod.Delete, bucketName, objectName).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception {e.Message}");
        }
    }

    private static async Task GetPresignedUrlByRequest(IMinioClient client,
        GetPresignedUrlArgs.PresignedUrlHttpMethod requestMethod,
        string bucketName = "my-bucket-name",
        string objectName = "my-object-name")
    {
        var args = new GetPresignedUrlArgs(requestMethod)
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithExpiry(1000);
        var presignedUrl = await client.GetPresignedUrlAsync(args).ConfigureAwait(false);
        Console.WriteLine($"Presigned '{requestMethod}' URL: {presignedUrl}");
    }
}
