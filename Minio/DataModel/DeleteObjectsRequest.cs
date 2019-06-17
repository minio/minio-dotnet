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
    [Serializable]
    [XmlType(TypeName = "Delete")]
    public class DeleteObjectsRequest
    {
        [XmlElement("Quiet")]
        public bool Quiet { get; set; }
        [XmlElement("Object")]
        public List<DeleteObject> Objects { get; set; }

        public DeleteObjectsRequest(List<DeleteObject> objectsList, bool quiet = true)
        {
            this.Quiet = quiet;
            this.Objects = objectsList;
        }

        public DeleteObjectsRequest()
        {
            this.Quiet = true;
            this.Objects = new List<DeleteObject>();
        }
    }
}
