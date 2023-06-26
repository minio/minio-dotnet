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
using Minio.Helper;

namespace Minio.DataModel.Args
{
    public class MakeBucketArgs : BucketArgs<MakeBucketArgs>
    {
        public MakeBucketArgs()
        {
            RequestMethod = HttpMethod.Put;
        }

        internal string Location { get; set; }
        internal bool ObjectLock { get; set; }

        public MakeBucketArgs WithLocation(string loc)
        {
            Location = loc;
            return this;
        }

        public MakeBucketArgs WithObjectLock()
        {
            ObjectLock = true;
            return this;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {
            // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
            if (!string.IsNullOrEmpty(Location) &&
                !string.Equals(Location, "us-east-1", StringComparison.OrdinalIgnoreCase))
            {
                var config = new CreateBucketConfiguration(Location);
                var body = Utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
                requestMessageBuilder.AddXmlBody(body);
                requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
                    Utils.GetMD5SumStr(Encoding.UTF8.GetBytes(body)));
            }

            if (ObjectLock)
            {
                requestMessageBuilder.AddOrUpdateHeaderParameter("X-Amz-Bucket-Object-Lock-Enabled", "true");
            }

            return requestMessageBuilder;
        }
    }
}