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
using Minio.DataModel.ObjectLock;
using Minio.Helper;

namespace Minio.DataModel.Args;

public class SetObjectLockConfigurationArgs : BucketArgs<SetObjectLockConfigurationArgs>
{
    public SetObjectLockConfigurationArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal ObjectLockConfiguration LockConfiguration { set; get; }

    public SetObjectLockConfigurationArgs WithLockConfiguration(ObjectLockConfiguration config)
    {
        LockConfiguration = config;
        return this;
    }

    internal override void Validate()
    {
        base.Validate();
        if (LockConfiguration is null)
            throw new InvalidOperationException("The lock configuration object " + nameof(LockConfiguration) +
                                                " is not set. Please use " + nameof(WithLockConfiguration) +
                                                " to set.");
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("object-lock", "");
        var body = Utils.MarshalXML(LockConfiguration, "http://s3.amazonaws.com/doc/2006-03-01/");
        // Convert string to a byte array
        // byte[] bodyInBytes = Encoding.ASCII.GetBytes(body);

        // requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
        // requestMessageBuilder.SetBody(bodyInBytes);
        //
        // string body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
        requestMessageBuilder.AddXmlBody(body);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            Utils.GetMD5SumStr(Encoding.UTF8.GetBytes(body)));
        //
        return requestMessageBuilder;
    }
}
