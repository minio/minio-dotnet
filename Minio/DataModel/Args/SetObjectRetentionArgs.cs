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

namespace Minio.DataModel.Args
{
    public class SetObjectRetentionArgs : ObjectVersionArgs<SetObjectRetentionArgs>
    {
        public SetObjectRetentionArgs()
        {
            RequestMethod = HttpMethod.Put;
            RetentionUntilDate = default;
            Mode = ObjectRetentionMode.GOVERNANCE;
        }

        internal bool BypassGovernanceMode { get; set; }
        internal ObjectRetentionMode Mode { get; set; }
        internal DateTime RetentionUntilDate { get; set; }

        internal override void Validate()
        {
            base.Validate();
            if (RetentionUntilDate.Equals(default))
            {
                throw new InvalidOperationException("Retention Period is not set. Please set using " +
                                                    nameof(WithRetentionUntilDate) + ".");
            }

            if (DateTime.Compare(RetentionUntilDate, DateTime.Now) <= 0)
            {
                throw new InvalidOperationException("Retention until date set using " + nameof(WithRetentionUntilDate) +
                                                    " needs to be in the future.");
            }
        }

        public SetObjectRetentionArgs WithBypassGovernanceMode(bool bypass = true)
        {
            BypassGovernanceMode = bypass;
            return this;
        }

        public SetObjectRetentionArgs WithRetentionMode(ObjectRetentionMode mode)
        {
            Mode = mode;
            return this;
        }

        public SetObjectRetentionArgs WithRetentionUntilDate(DateTime date)
        {
            RetentionUntilDate = date;
            return this;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {
            requestMessageBuilder.AddQueryParameter("retention", "");
            if (!string.IsNullOrEmpty(VersionId))
            {
                requestMessageBuilder.AddQueryParameter("versionId", VersionId);
            }

            if (BypassGovernanceMode)
            {
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-bypass-governance-retention", "true");
            }

            var config = new ObjectRetentionConfiguration(RetentionUntilDate, Mode);
            var body = Utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
            requestMessageBuilder.AddXmlBody(body);
            requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
                Utils.GetMD5SumStr(Encoding.UTF8.GetBytes(body)));
            return requestMessageBuilder;
        }
    }
}