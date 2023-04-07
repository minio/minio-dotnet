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
    private readonly Dictionary<string, string> _headers = new();
    private string _content;
    private Memory<byte> _contentBytes;

    private Stream _stream;

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
            if (Response == null) return 0;

            return Response.StatusCode;
        }
    }

    public Stream ContentStream
    {
        get
        {
            if (Response == null) return null;

            return _stream ??= Response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        }
    }

    public ReadOnlyMemory<byte> ContentBytes
    {
        get
        {
            if (ContentStream == null)
                return Array.Empty<byte>();

            if (_contentBytes.IsEmpty)
            {
                using var memoryStream = new MemoryStream();
                ContentStream.CopyTo(memoryStream);
                _contentBytes = memoryStream.ToArray();
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

    public Dictionary<string, string> Headers
    {
        get
        {
            if (Response == null) return new Dictionary<string, string>();

            if (!_headers.Any())
            {
                if (Response.Content != null)
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
        _stream?.Dispose();
        Request?.Dispose();
        Response?.Dispose();
    }
}