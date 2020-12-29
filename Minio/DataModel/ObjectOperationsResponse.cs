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
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using RestSharp;

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


    internal class StatObjectResponse : GenericResponse
    {
        internal ObjectStat ObjectInfo { get; set; }
        internal StatObjectResponse(HttpStatusCode statusCode, string responseContent, IList<Parameter> responseHeaders, StatObjectArgs args)
                    : base(statusCode, responseContent)
        {
            // StatObjectResponse object is populated with available stats from the response.
            this.ObjectInfo = ObjectStat.FromResponseHeaders(args.ObjectName, responseHeaders);
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

    internal class PresignedPostPolicyResponse
    {
        internal Tuple<string, Dictionary<string, string>> URIPolicyTuple { get; private set; }

        public PresignedPostPolicyResponse(PresignedPostPolicyArgs args, string absURI)
        {
            args.Policy.SetAlgorithm("AWS4-HMAC-SHA256");
            args.Policy.SetDate(DateTime.UtcNow);
            args.Policy.SetPolicy(args.Policy.Base64());
            URIPolicyTuple = Tuple.Create(absURI, args.Policy.GetFormData());
        }
    }

    public class GetLegalHoldResponse: GenericResponse
    {
        internal ObjectLegalHoldConfiguration CurrentLegalHoldConfiguration { get; private set; }
        internal string Status { get; private set;}
        public GetLegalHoldResponse(HttpStatusCode statusCode, string responseContent)
            : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) || !HttpStatusCode.OK.Equals(statusCode))
            {
                this.CurrentLegalHoldConfiguration = null;
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                CurrentLegalHoldConfiguration = (ObjectLegalHoldConfiguration)new XmlSerializer(typeof(ObjectLegalHoldConfiguration)).Deserialize(stream);
            }
            if ( this.CurrentLegalHoldConfiguration == null
                    || string.IsNullOrEmpty(this.CurrentLegalHoldConfiguration.Status) )
            {
                Status = "OFF";
            }
            else
            {
                Status = this.CurrentLegalHoldConfiguration.Status;
            }
        }
    }

    internal class GetObjectTagsResponse : GenericResponse
    {
        public GetObjectTagsResponse(HttpStatusCode statusCode, string responseContent)
            : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                this.ObjectTags = null;
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.ObjectTags = (Tagging)new XmlSerializer(typeof(Tagging)).Deserialize(stream);
            }
        }

        public Tagging ObjectTags { get; set; }
    }

    internal class GetRetentionResponse: GenericResponse
    {
        internal ObjectRetentionConfiguration CurrentRetentionConfiguration { get; private set; }
        public GetRetentionResponse(HttpStatusCode statusCode, string responseContent)
            : base(statusCode, responseContent)
        {
            if ( string.IsNullOrEmpty(responseContent) && !HttpStatusCode.OK.Equals(statusCode))
            {
                this.CurrentRetentionConfiguration = null;
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                CurrentRetentionConfiguration = (ObjectRetentionConfiguration)new XmlSerializer(typeof(ObjectRetentionConfiguration)).Deserialize(stream);
            }
        }
    }
}