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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Minio;

public class ResponseResult : IDisposable
{
    private IReadOnlyDictionary<string, string> _headers = null;
    private string _content;
    private byte[] _contentBytes;

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

            return _stream ?? (_stream = Response.Content.ReadAsStreamAsync().Result);
        }
    }

    public byte[] ContentBytes
    {
        get
        {
            if (ContentStream == null) return new byte[0];

            if (_contentBytes == null)
                using (var memoryStream = new MemoryStream())
                {
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

            if (_content == null) _content = Encoding.UTF8.GetString(ContentBytes);

            return _content;
        }
    }

    /// <summary>
    /// Returns the response headers. The returned dictionary uses case-insensitive comparer on
    /// the keys based on rfc2616.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers
    {
        get
        {
            if (_headers == null)
            {
                _headers = GetHeaders(Response);
            }

            return _headers;
        }
    }

    /// <summary>
    /// Adds each header to the internal header dictionary.
    /// </summary>
    /// <param name="response"></param>
    private static IReadOnlyDictionary<string, string> GetHeaders(HttpResponseMessage response)
    {
        // Headers keys are case-insensitive, see https://www.w3.org/Protocols/rfc2616/rfc2616-sec4.html#sec4.2
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddHeaders(headers, response?.Content?.Headers);
        AddHeaders(headers, response?.Headers);

        return headers;
    }

    private static void AddHeaders(Dictionary<string, string> target, HttpHeaders source)
    {
        if (source == null) return;
        foreach (var header in source) target.Add(header.Key, header.Value.FirstOrDefault());
    }

    public string ErrorMessage => Exception?.Message;

    public void Dispose()
    {
        _stream?.Dispose();
        Request?.Dispose();
        Response?.Dispose();
    }
}