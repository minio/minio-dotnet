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
using Minio.DataModel.Replication;

namespace Minio.DataModel.Args;

public class SetBucketReplicationArgs : BucketArgs<SetBucketReplicationArgs>
{
    public SetBucketReplicationArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal ReplicationConfiguration BucketReplication { get; private set; }

    public SetBucketReplicationArgs WithConfiguration(ReplicationConfiguration conf)
    {
        BucketReplication = conf;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("replication", "");
        var body = BucketReplication.MarshalXML();
        // Convert string to a byte array
        ReadOnlyMemory<byte> bodyInBytes = Encoding.ASCII.GetBytes(body);
        requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
        requestMessageBuilder.SetBody(bodyInBytes);

        return requestMessageBuilder;
    }
}
