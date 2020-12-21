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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Minio.DataModel;
using System.IO;
using RestSharp;

namespace Minio
{
    public partial class MinioClient : IObjectOperations
    {
        /// <summary>
        /// private helper method to remove list of objects from bucket
        /// </summary>
        /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name etc </param>
        /// <param name="objectStat"> ObjectStat object encapsulates information like - object name, size, etag etc </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        private async Task getObjectFileAsync(GetObjectArgs args, ObjectStat objectStat, CancellationToken cancellationToken = default(CancellationToken))
        {
            long length = objectStat.Size;
            string etag = objectStat.ETag;

            long tempFileSize = 0;
            string tempFileName = $"{args.FileName}.{etag}.part.minio";
            if (!string.IsNullOrEmpty(args.VersionId))
            {
                tempFileName = $"{args.FileName}.{etag}.{args.VersionId}.part.minio";
            }
            if (File.Exists(args.FileName))
            {
                File.Delete(args.FileName);
            }

            utils.ValidateFile(tempFileName);
            if (File.Exists(tempFileName))
            {
                File.Delete(tempFileName);
            }

            args = args.WithCallbackStream( (stream) =>
                                    {
                                        var fileStream = File.Create(tempFileName);
                                        stream.CopyTo(fileStream);
                                        fileStream.Dispose();
                                        FileInfo writtenInfo = new FileInfo(tempFileName);
                                        long writtenSize = writtenInfo.Length;
                                        if (writtenSize != (length - tempFileSize))
                                        {
                                            throw new IOException(tempFileName + ": unexpected data written.  expected = " + (length - tempFileSize)
                                                                + ", written = " + writtenSize);
                                        }
                                        utils.MoveWithReplace(tempFileName, args.FileName);
                                    });
            await getObjectStreamAsync(args, objectStat, null, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// private helper method to remove list of objects from bucket
        /// </summary>
        /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name etc </param>
        /// <param name="objectStat"> ObjectStat object encapsulates information like - object name, size, etag etc, represents Object Information </param>
        /// <param name="cb"> Action object of type Stream, callback to send Object contents, if assigned </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        private async Task getObjectStreamAsync(GetObjectArgs args, ObjectStat objectStat, Action<Stream> cb, CancellationToken cancellationToken = default(CancellationToken))
        {
            RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
            await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken);
        }
    }

    public class OperationsUtil
    {
        private static readonly List<string> SupportedHeaders = new List<string> { "cache-control", "content-encoding", "content-type", "x-amz-acl", "content-disposition" };
        internal static bool IsSupportedHeader(string hdr, IEqualityComparer<string> comparer = null)
        {
            comparer = comparer ?? StringComparer.OrdinalIgnoreCase;
            return SupportedHeaders.Contains(hdr, comparer);
        }
    }
}
