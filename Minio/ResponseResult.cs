using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Minio
{
    public class ResponseResult
    {
        private Exception Exception { get; }
        public HttpRequestMessage Request { get; }
        private HttpResponseMessage Response { get; }

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

        public Dictionary<string,string> Headers
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
    }
}using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Minio
{
    public class ResponseResult
    {
        private Exception Exception { get; }
        public HttpRequestMessage Request { get; }
        private HttpResponseMessage Response { get; }

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

        public Dictionary<string,string> Headers
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
    }
}