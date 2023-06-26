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

using Minio.Helper;

namespace Minio.DataModel.Args;

internal class PutObjectPartArgs : PutObjectArgs
{
    public PutObjectPartArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal override void Validate()
    {
        base.Validate();
        if (string.IsNullOrWhiteSpace(UploadId))
            throw new ArgumentNullException(nameof(UploadId) + " not assigned for PutObjectPart operation.",
                nameof(UploadId));
    }

    public new PutObjectPartArgs WithBucket(string bkt)
    {
        return (PutObjectPartArgs)base.WithBucket(bkt);
    }

    public new PutObjectPartArgs WithObject(string obj)
    {
        return (PutObjectPartArgs)base.WithObject(obj);
    }

    public new PutObjectPartArgs WithObjectSize(long size)
    {
        return (PutObjectPartArgs)base.WithObjectSize(size);
    }

    public new PutObjectPartArgs WithHeaders(IDictionary<string, string> hdr)
    {
        return (PutObjectPartArgs)base.WithHeaders(hdr);
    }

    public PutObjectPartArgs WithRequestBody(object data)
    {
        return (PutObjectPartArgs)base.WithRequestBody(Utils.ObjectToByteArray(data));
    }

    public new PutObjectPartArgs WithStreamData(Stream data)
    {
        return (PutObjectPartArgs)base.WithStreamData(data);
    }

    public new PutObjectPartArgs WithContentType(string type)
    {
        return (PutObjectPartArgs)base.WithContentType(type);
    }

    public new PutObjectPartArgs WithUploadId(string id)
    {
        return (PutObjectPartArgs)base.WithUploadId(id);
    }

    public new PutObjectPartArgs WithProgress(IProgress<ProgressReport> progress)
    {
        return (PutObjectPartArgs)base.WithProgress(progress);
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        return requestMessageBuilder;
    }
}