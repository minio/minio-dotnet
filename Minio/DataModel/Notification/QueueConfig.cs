/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Minio.DataModel
{
    // QueueConfig carries one single queue notification configuration
    [Serializable]
    public class QueueConfig : NotificationConfiguration
    {
        public string Queue { get; set; }

        public QueueConfig():base()
        {
        }

        public QueueConfig(string arn): base(arn)
        {
            this.Queue = arn.ToString();
        }

        public QueueConfig(Arn arn) : base(arn)
        {
            this.Queue = arn.ToString();
        }
        // Implement equality for this object
        public override bool Equals(Object obj)
        {         
            QueueConfig other = (QueueConfig)obj;
            // If parameter is null return false.
            if (other == null)
            {
                return false;
            }
           return other.Queue.Equals(this.Queue);
  
        }

        public override int GetHashCode()
        {
            return this.Queue.GetHashCode();
        }
    }
}
