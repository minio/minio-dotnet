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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Minio;

public class ResponseHeaderCollection : IReadOnlyDictionary<string, string>
{
    private readonly Dictionary<string, string> _headers;

    public static readonly ResponseHeaderCollection Empty = new ResponseHeaderCollection();

    private ResponseHeaderCollection()
    {
        _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public ResponseHeaderCollection(HttpResponseMessage response)
    {
        _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (response.Content != null)
        {
            foreach (var header in response.Content.Headers)
            {
                var value = header.Value.FirstOrDefault();
                if (value != null)
                {
                    _headers.Add(header.Key, value);
                }
            }
        }

        foreach (var header in response.Headers)
        {
            // if the content header already has this header value,
            // avoid ArgumentException if the key already exists
            if (!_headers.ContainsKey(header.Key))
            {
                var value = header.Value.FirstOrDefault();
                if (value != null)
                {
                    _headers.Add(header.Key, value);
                }
            }
        }
    }

    public string this[string key] => _headers[key];

    public IEnumerable<string> Keys => _headers.Keys;

    public IEnumerable<string> Values => _headers.Values;

    public int Count => _headers.Count;

    public bool ContainsKey(string key) => _headers.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _headers.GetEnumerator();

    public bool TryGetValue(string key, out string value) => _headers.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => _headers.GetEnumerator();
}
