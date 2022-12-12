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
using Minio.Exceptions;

namespace Minio.Examples.Cases;

internal class StatObjectQuery
{
    public static void PrintStat(string bucketObject, ObjectStat statObject)
    {
        var currentColor = Console.ForegroundColor;
        Console.WriteLine($"Details of the object {bucketObject} are");
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"{statObject}");
        Console.ForegroundColor = currentColor;
        Console.WriteLine();
    }

    // Get stats on a object
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        string bucketObject = "my-object-name",
        string versionID = null,
        string matchEtag = null,
        DateTime modifiedSince = default)
    {
        try
        {
            Console.WriteLine("Running example for API: StatObjectAsync [with ObjectQuery]");

            var args = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(bucketObject)
                .WithVersionId(versionID)
                .WithMatchETag(matchEtag)
                .WithModifiedSince(modifiedSince);
            var statObjectVersion = await minio.StatObjectAsync(args);
            PrintStat(bucketObject, statObjectVersion);
        }
        catch (MinioException me)
        {
            var objectNameInfo = $"{bucketName}-{bucketObject}";
            if (!string.IsNullOrEmpty(versionID))
                objectNameInfo = objectNameInfo +
                                 $" (Version ID) {me.Response.VersionId} (Marked DEL) {me.Response.DeleteMarker}";
            Console.WriteLine($"[StatObject] {objectNameInfo} Exception: {me}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[StatObject]  Exception: {e}");
        }
    }
}