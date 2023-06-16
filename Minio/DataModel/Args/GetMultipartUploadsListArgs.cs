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

using System.Globalization;

namespace Minio.DataModel.Args;

public class GetMultipartUploadsListArgs : BucketArgs<GetMultipartUploadsListArgs>
{
    public GetMultipartUploadsListArgs()
    {
        RequestMethod = HttpMethod.Get;
        MAX_UPLOAD_COUNT = 1000;
    }

    internal string Prefix { get; private set; }
    internal string Delimiter { get; private set; }
    internal string KeyMarker { get; private set; }
    internal string UploadIdMarker { get; private set; }
    internal uint MAX_UPLOAD_COUNT { get; }

    public GetMultipartUploadsListArgs WithPrefix(string prefix)
    {
        Prefix = prefix ?? string.Empty;
        return this;
    }

    public GetMultipartUploadsListArgs WithDelimiter(string delim)
    {
        Delimiter = delim ?? string.Empty;
        return this;
    }

    public GetMultipartUploadsListArgs WithKeyMarker(string nextKeyMarker)
    {
        KeyMarker = nextKeyMarker ?? string.Empty;
        return this;
    }

    public GetMultipartUploadsListArgs WithUploadIdMarker(string nextUploadIdMarker)
    {
        UploadIdMarker = nextUploadIdMarker ?? string.Empty;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploads", "");
        requestMessageBuilder.AddQueryParameter("prefix", Prefix);
        requestMessageBuilder.AddQueryParameter("delimiter", Delimiter);
        requestMessageBuilder.AddQueryParameter("key-marker", KeyMarker);
        requestMessageBuilder.AddQueryParameter("upload-id-marker", UploadIdMarker);
        requestMessageBuilder.AddQueryParameter("max-uploads", MAX_UPLOAD_COUNT.ToString(CultureInfo.InvariantCulture));
        return requestMessageBuilder;
    }
}