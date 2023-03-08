/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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
using Minio.DataModel;
using Minio.Exceptions;

namespace Minio;

public class GetVersioningArgs : BucketArgs<GetVersioningArgs>
{
    public GetVersioningArgs()
    {
        RequestMethod = HttpMethod.Get;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("versioning", "");
        return requestMessageBuilder;
    }
}

public class SetVersioningArgs : BucketArgs<SetVersioningArgs>
{
    internal VersioningStatus CurrentVersioningStatus;

    public SetVersioningArgs()
    {
        RequestMethod = HttpMethod.Put;
        CurrentVersioningStatus = VersioningStatus.Off;
    }

    internal override void Validate()
    {
        Utils.ValidateBucketName(BucketName);
        if (CurrentVersioningStatus > VersioningStatus.Suspended)
            throw new UnexpectedMinioException("CurrentVersioningStatus invalid value .");
    }

    public SetVersioningArgs WithVersioningEnabled()
    {
        CurrentVersioningStatus = VersioningStatus.Enabled;
        return this;
    }

    public SetVersioningArgs WithVersioningSuspended()
    {
        CurrentVersioningStatus = VersioningStatus.Suspended;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        var config = new VersioningConfiguration(CurrentVersioningStatus == VersioningStatus.Enabled);

        var body = Utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
        requestMessageBuilder.AddXmlBody(body);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            Utils.GetMD5SumStr(Encoding.UTF8.GetBytes(body)));

        requestMessageBuilder.AddQueryParameter("versioning", "");
        return requestMessageBuilder;
    }

    internal enum VersioningStatus : ushort
    {
        Off = 0,
        Enabled = 1,
        Suspended = 2
    }
}