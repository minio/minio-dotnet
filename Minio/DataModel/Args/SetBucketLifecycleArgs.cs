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

using System.Text;
using Minio.DataModel.ILM;
using Minio.Helper;

namespace Minio.DataModel.Args;

public class SetBucketLifecycleArgs : BucketArgs<SetBucketLifecycleArgs>
{
    public SetBucketLifecycleArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal LifecycleConfiguration BucketLifecycle { get; private set; }

    public SetBucketLifecycleArgs WithLifecycleConfiguration(LifecycleConfiguration lc)
    {
        BucketLifecycle = lc;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("lifecycle", "");
        var body = BucketLifecycle.MarshalXML();
        // Convert string to a byte array
        ReadOnlyMemory<byte> bodyInBytes = Encoding.ASCII.GetBytes(body);
        requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
        requestMessageBuilder.SetBody(bodyInBytes);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            Utils.GetMD5SumStr(bodyInBytes.Span));

        return requestMessageBuilder;
    }

    internal override void Validate()
    {
        base.Validate();
        if (BucketLifecycle is null || BucketLifecycle.Rules.Count == 0)
            throw new InvalidOperationException("Unable to set empty Lifecycle configuration.");
    }
}
