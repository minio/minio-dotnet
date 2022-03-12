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
using System.Text;

namespace Minio
{
    public class ResponseResult : IDisposable
    {
        private Exception Exception { get; }
        public HttpRequestMessage Request { get; }
        public HttpResponseMessage Response { get; }

        public HttpStatusCode StatusCode
        {
            get
            {
                if (this.Response == null)
                {
                    return 0;
                }

                return this.Response.StatusCode;
            }
        }

        private Stream _stream;
        private byte[] _contentBytes;
        private string _content;

        public Stream ContentStream
        {
            get
            {
                if (this.Response == null)
                {
                    return null;
                }

                return _stream ?? (_stream = this.Response.Content.ReadAsStreamAsync().Result);
            }
        }

        public byte[] ContentBytes
        {
            get
            {
                if (this.ContentStream == null)
                {
                    return new byte[0];
                }

                if (_contentBytes == null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        this.ContentStream.CopyTo(memoryStream);
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
                if (this.ContentBytes.Length == 0)
                {
                    return "";
                }

                if (this._content == null)
                {
                    _content = Encoding.UTF8.GetString(this.ContentBytes);
                }

                return _content;
            }
        }

        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();

        public Dictionary<string, string> Headers
        {
            get
            {
                if (this.Response == null)
                {
                    return new Dictionary<string, string>();
                }

                if (!_headers.Any())
                {
                    if (this.Response.Content != null)
                    {
                        foreach (var item in this.Response.Content.Headers)
                        {
                            _headers.Add(item.Key, item.Value.FirstOrDefault());
                        }
                    }

                    foreach (var item in this.Response.Headers)
                    {
                        _headers.Add(item.Key, item.Value.FirstOrDefault());
                    }
                }

                return _headers;
            }
        }

        public string ErrorMessage => this.Exception?.Message;

        public ResponseResult(HttpRequestMessage request, HttpResponseMessage response)
        {
            this.Request = request;
            this.Response = response;
        }

        public ResponseResult(HttpRequestMessage request, Exception exception)
            : this(request, response: null)
        {
            this.Exception = exception;
        }

        public void Dispose()
        {
            _stream?.Dispose();
            Request?.Dispose();
            Response?.Dispose();
        }
    }
}