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
using Minio.DataModel;

using System;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class SelectObjectContent
    {
        // Get object in a bucket
        public async static Task Run(MinioClient minio,
                                     string bucketName = "my-bucket-name",
                                     string objectName = "my-object-name",
                                     string fileName = "my-file-name")
        {
            try
            {
                Console.WriteLine("Running example for API: SelectObjectContentAsync");
                var opts = new SelectObjectOptions()
                {
                    ExpressionType = QueryExpressionType.SQL,
                    Expression = "select count(*) from s3object",
                    InputSerialization = new SelectObjectInputSerialization()
                    {
                        CompressionType = SelectCompressionType.NONE,
                        CSV = new CSVInputOptions()
                        {
                            FileHeaderInfo = CSVFileHeaderInfo.None,
				            RecordDelimiter = "\n",
				            FieldDelimiter = ",",
                        }                    
                    },
                    OutputSerialization = new SelectObjectOutputSerialization()
                    {
                        CSV = new CSVOutputOptions()
                        {
                            RecordDelimiter = "\n",
                            FieldDelimiter =  ",",
                        }
                    }
                };

                SelectObjectContentArgs args = new SelectObjectContentArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithSelectObjectOptions(opts);
                var resp = await minio.SelectObjectContentAsync(args);
                resp.Payload.CopyTo(Console.OpenStandardOutput());
                Console.WriteLine("Bytes scanned:" + resp.Stats.BytesScanned);
                Console.WriteLine("Bytes returned:" + resp.Stats.BytesReturned);
                Console.WriteLine("Bytes processed:" + resp.Stats.BytesProcessed);
                if (resp.Progress != null)
                {
                    Console.WriteLine("Progress :" + resp.Progress.BytesProcessed);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }
}
