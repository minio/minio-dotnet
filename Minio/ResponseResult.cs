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
using System.Net;
using System.Net.Http;
using System.Text;

namespace Minio;

public class ResponseResult : IDisposable
{
    private ResponseHeaderCollection _headers;
    private string _content;
    private byte[] _contentBytes;

    public ResponseResult(HttpRequestMessage request, HttpResponseMessage response)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Response = response ?? throw new ArgumentNullException(nameof(response));

        _headers = new ResponseHeaderCollection(response);
    }

    public ResponseResult(HttpRequestMessage request, Exception exception)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        _headers = ResponseHeaderCollection.Empty;
    }

    private Exception Exception { get; }
    public HttpRequestMessage Request { get; }

    /// <summary>
    /// The response or null of there was an exception.
    /// </summary>
    public HttpResponseMessage Response { get; }

    public HttpStatusCode StatusCode => Response?.StatusCode ?? 0;

    /// <summary>
    /// Gets the response content as a byte array. If there is no response or content, returns empty array.
    /// </summary>
    public byte[] ContentBytes
    {
        get
        {
            if (_contentBytes == null)
            {
                if (Response?.Content == null) return Array.Empty<byte>();

                using (var stream = Response.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult())
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    _contentBytes = memoryStream.ToArray();
                }
            }

            return _contentBytes;
        }
    }

    public string Content
    {
        get
        {
            if (ContentBytes.Length == 0) return "";

            _content ??= Encoding.UTF8.GetString(ContentBytes);

            return _content;
        }
    }

    public IReadOnlyDictionary<string, string> Headers => _headers;

    public string ErrorMessage => Exception?.Message;

    public void Dispose()
    {
        Request?.Dispose();
        Response?.Dispose();
    }
}
