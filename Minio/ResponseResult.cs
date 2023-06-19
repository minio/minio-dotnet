/*
* MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
* (C) 2017-2021 MinIO, Inc.
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

using System.Net;
using System.Text;

namespace Minio;

public class ResponseResult : IDisposable
{
    private readonly Dictionary<string, string> _headers = new(StringComparer.Ordinal);
    private string _content;
    private ReadOnlyMemory<byte> _contentBytes;

    private Stream _stream;
    private bool disposedValue;

    public ResponseResult(HttpRequestMessage request, HttpResponseMessage response)
    {
        Request = request;
        Response = response;
    }

    public ResponseResult(HttpRequestMessage request, Exception exception)
        : this(request, response: null)
    {
        Exception = exception;
    }

    private Exception Exception { get; }
    public HttpRequestMessage Request { get; }
    public HttpResponseMessage Response { get; }

    public HttpStatusCode StatusCode
    {
        get
        {
#pragma warning disable MA0099 // Use Explicit enum value instead of 0
            if (Response is null) return 0;
#pragma warning restore MA0099 // Use Explicit enum value instead of 0

            return Response.StatusCode;
        }
    }

    public Stream ContentStream
    {
        get
        {
            if (Response is null) return null;
#if NETSTANDARD
            return _stream ??= Response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
#else
            return _stream ??= Response.Content.ReadAsStream();
#endif
        }
    }

    public ReadOnlyMemory<byte> ContentBytes
    {
        get
        {
            if (ContentStream is null)
                return ReadOnlyMemory<byte>.Empty;

            if (_contentBytes.IsEmpty)
            {
                using var memoryStream = new MemoryStream();
                ContentStream.CopyTo(memoryStream);
                _contentBytes = new ReadOnlyMemory<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
            }

            return _contentBytes;
        }
    }

    public string Content
    {
        get
        {
            if (ContentBytes.Length == 0) return "";
#if NETSTANDARD
            _content ??= Encoding.UTF8.GetString(ContentBytes.ToArray());
#else
            _content ??= Encoding.UTF8.GetString(ContentBytes.Span);
#endif
            return _content;
        }
    }

    public IDictionary<string, string> Headers
    {
        get
        {
            if (Response is null) return new Dictionary<string, string>(StringComparer.Ordinal);

            if (!_headers.Any())
            {
                if (Response.Content is not null)
                    foreach (var item in Response.Content.Headers)
                        _headers.Add(item.Key, item.Value.FirstOrDefault());

                foreach (var item in Response.Headers) _headers.Add(item.Key, item.Value.FirstOrDefault());
            }

            return _headers;
        }
    }

    public string ErrorMessage => Exception?.Message;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _stream?.Dispose();
                Request?.Dispose();
                Response?.Dispose();

                _content = null;
                _contentBytes = null;
                _stream = null;
            }

            disposedValue = true;
        }
    }
}