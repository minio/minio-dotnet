/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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

namespace Minio.DataModel.Notification
{
    using System.Xml.Serialization;

    // Arn holds ARN information that will be sent to the web service,
    // ARN desciption can be found in http://docs.aws.amazon.com/general/latest/gr/aws-arns-and-namespaces.html
    public class Arn
    {
        // Pass valid Arn string on aws to constructor
        public Arn(string arnString)
        {
            var parts = arnString.Split(':');
            if (parts.Length != 6)
            {
                return;
            }
            
            this.Partition = parts[1];
            this.Service = parts[2];
            this.Region = parts[3];
            this.AccountId = parts[4];
            this.Resource = parts[5];
            this.ArnString = arnString;
        }

        // constructs new ARN based on the given partition, service, region, account id and resource
        public Arn(string partition, string service, string region, string accountId, string resource)
        {
            this.Partition = partition;
            this.Service = service;
            this.Region = region;
            this.AccountId = accountId;
            this.Resource = resource;
            this.ArnString = "arn:" + this.Partition + ":" + this.Service + ":" + this.Region + ":" + this.AccountId +
                             ":" + this.Resource;
        }

        [XmlText]
        public string ArnString { get; }

        public string Partition { get; }
        public string Service { get; }
        public string Region { get; }
        public string AccountId { get; }
        public string Resource { get; }

        public override string ToString()
        {
            return this.ArnString;
        }
    }
}