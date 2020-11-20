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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.Xml.Serialization;

using Minio.DataModel;

namespace Minio
{
    internal class SelectObjectContentResponse : GenericResponse
    {
        internal SelectResponseStream ResponseStream { get; private set; }
        internal SelectObjectContentResponse(HttpStatusCode statusCode, string responseContent, byte[] responseRawBytes)
                    : base(statusCode, responseContent)
        {
            this.ResponseStream = new SelectResponseStream(new MemoryStream(responseRawBytes));
        }

    }

    internal class GetMultipartUploadsListResponse : GenericResponse
    {
        internal Tuple<ListMultipartUploadsResult, List<Upload>> UploadResult { get; private set; }
        internal GetMultipartUploadsListResponse(HttpStatusCode statusCode, string responseContent)
                    : base(statusCode, responseContent)
        {
            ListMultipartUploadsResult uploadsResult = null;
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                uploadsResult = (ListMultipartUploadsResult)new XmlSerializer(typeof(ListMultipartUploadsResult)).Deserialize(stream);
            }
            XDocument root = XDocument.Parse(responseContent);
            var itemCheck = root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Upload").FirstOrDefault();
            if (uploadsResult == null || itemCheck == null || !itemCheck.HasElements)
            {
                return;
            }
            var uploads  = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Upload")
                          select new Upload
                          {
                              Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                              UploadId = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}UploadId").Value,
                              Initiated = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Initiated").Value
                          };
            this.UploadResult = new Tuple<ListMultipartUploadsResult, List<Upload>>(uploadsResult, uploads.ToList());
        }
    }
}
