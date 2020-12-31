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
using Minio.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class StatObject
    {
        public static void PrintStat(string bucketObject, ObjectStat statObject)
        {
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.WriteLine($"Details of the object {bucketObject} are");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"{statObject}");
            Console.ForegroundColor = currentColor;
            Console.WriteLine();
        }
        // Get stats on a object
        public async static Task Run(MinioClient minio, 
                                     string bucketName = "my-bucket-name",
                                     string bucketObject = "my-object-name",
                                     string versionID = null)
        {
            try
            {
                Console.WriteLine("Running example for API: StatObjectAsync");
                if (string.IsNullOrEmpty(versionID))
                {
                    StatObjectArgs objectStatArgs = new StatObjectArgs()
                                                    .WithBucket(bucketName)
                                                    .WithObject(bucketObject);
                    ObjectStat statObject = await minio.StatObjectAsync(objectStatArgs);
                    PrintStat(bucketObject, statObject);
                    PrintMetaData(statObject.MetaData);
                    return;
                }
                StatObjectArgs args = new StatObjectArgs()
                                                .WithBucket(bucketName)
                                                .WithObject(bucketObject)
                                                .WithVersionId(versionID);
                ObjectStat statObjectVersion = await minio.StatObjectAsync(args);
                PrintStat(bucketObject, statObjectVersion);
                PrintMetaData(statObjectVersion.MetaData);
            }
            catch (MinioException me)
            {
                string objectNameInfo = $"{bucketName}-{bucketObject}";
                if (!string.IsNullOrEmpty(versionID))
                {
                    objectNameInfo = objectNameInfo + $" (Version ID) {me.Response.VersionId} (Delete Marker) {me.Response.DeleteMarker}";
                }
                Console.WriteLine($"[StatObject] {objectNameInfo} Exception: {me}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[StatObject] {bucketName}-{bucketObject} Exception: {e}");
            }
        }

        private static void PrintMetaData(Dictionary<string, string> metaData)
        {
            Console.WriteLine("Metadata:");
            foreach(var metaPair in metaData)
            {
                Console.WriteLine("    " + metaPair.Key + ":\t" + metaPair.Value);
            }
            Console.WriteLine();
        }
    }
}