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

namespace Minio.Helper;

public static class OperationsUtil
{
    private static readonly List<string> SupportedHeaders = new()
    {
        "cache-control", "content-encoding", "content-type",
        "x-amz-acl", "content-disposition", "x-minio-extract"
    };

    private static readonly List<string> SSEHeaders = new()
    {
        "X-Amz-Server-Side-Encryption-Customer-Algorithm",
        "X-Amz-Server-Side-Encryption-Customer-Key",
        "X-Amz-Server-Side-Encryption-Customer-Key-Md5",
        Constants.SSEGenericHeader,
        Constants.SSEKMSKeyId,
        Constants.SSEKMSContext
    };

    internal static bool IsSupportedHeader(string hdr, IEqualityComparer<string> comparer = null)
    {
        comparer ??= StringComparer.OrdinalIgnoreCase;
        return SupportedHeaders.Contains(hdr, comparer);
    }

    internal static bool IsSSEHeader(string hdr, IEqualityComparer<string> comparer = null)
    {
        comparer ??= StringComparer.OrdinalIgnoreCase;
        return SSEHeaders.Contains(hdr, comparer);
    }
}
