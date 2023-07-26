using System.Net;
using CommunityToolkit.HighPerformance;
using Minio.DataModel.Select;

/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020, 2021 MinIO, Inc.
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

namespace Minio.DataModel.Response;

internal class SelectObjectContentResponse : GenericResponse, IDisposable
{
    private bool disposed;

    internal SelectObjectContentResponse(HttpStatusCode statusCode, string responseContent,
        ReadOnlyMemory<byte> responseRawBytes)
        : base(statusCode, responseContent)
    {
        using var stream = responseRawBytes.AsStream();
        ResponseStream = new SelectResponseStream(stream);
    }

    internal SelectResponseStream ResponseStream { get; }

    public virtual void Dispose()
    {
        if (disposed) return;

        ResponseStream?.Dispose();

        disposed = true;
    }
}
