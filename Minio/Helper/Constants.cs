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

namespace Minio.Helper
{
    internal static class Constants
    {
        /// <summary>
        /// Maximum number of parts
        /// </summary>
        public static int MaxParts = 10000;

        /// <summary>
        /// Minimum part size
        /// </summary>
        public static long MinimumPartSize = 5 * 1024L * 1024L;

        /// <summary>
        /// Minimum PUT part size
        /// </summary>
        public static long MinimumPUTPartSize = 16 * 1024L * 1024L;

        /// <summary>
        /// Minimum COPY part size
        /// </summary>
        public static long MinimumCOPYPartSize = 512 * 1024L * 1024L;

        /// <summary>
        /// Maximum part size
        /// </summary>
        public static long MaximumPartSize = 5 * 1024L * 1024L * 1024L;

        /// <summary>
        /// Maximum streaming object size
        /// </summary>
        public static long MaximumStreamObjectSize = MaxParts * MinimumPartSize;

        /// <summary>
        /// maxSinglePutObjectSize - maximum size 5GiB of object per PUT operation
        /// </summary>
        public static long MaxSinglePutObjectSize = 1024L * 1024L * 1024L * 5;

        /// <summary>
        /// maxSingleCopyObjectSize - 5GiB
        /// </summary>
        public static long MaxSingleCopyObjectSize = 1024L * 1024L * 1024L * 5;

        /// <summary>
        /// maxMultipartPutObjectSize - maximum size 5TiB of object for Multipart operation
        /// </summary>
        public static long MaxMultipartPutObjectSize = 1024L * 1024L * 1024L * 1024L * 5;

        /// <summary>
        /// OptimalReadBufferSize - optimal buffer 5MiB used for reading through Read operation
        /// </summary>
        public static long OptimalReadBufferSize = 1024L * 1024L * 5;

        public static int DefaultExpiryTime = 7 * 24 * 3600;

        /// <summary>
        /// SSEGenericHeader is the AWS SSE header used for SSE-S3 and SSE-KMS.
        /// </summary>
        public static string SSEGenericHeader = "X-Amz-Server-Side-Encryption";

        /// <summary>
        /// SSEKMSKeyId is the AWS SSE KMS Key-Id
        /// </summary>
        public static string SSEKMSKeyId = "X-Amz-Server-Side-Encryption-Aws-Kms-Key-Id";

        /// <summary>
        /// SSEKMSContext is the AWS SSE KMS Context.
        /// </summary>
        public static string SSEKMSContext = "X-Amz-Server-Side-Encryption-Context";
    }
}
