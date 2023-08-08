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
using System.Xml.Linq;

namespace Minio.DataModel.Args;

internal class CompleteMultipartUploadArgs : ObjectWriteArgs<CompleteMultipartUploadArgs>
{
    internal CompleteMultipartUploadArgs()
    {
        RequestMethod = HttpMethod.Post;
    }

    internal CompleteMultipartUploadArgs(MultipartCopyUploadArgs args)
    {
        // destBucketName, destObjectName, metadata, sseHeaders
        RequestMethod = HttpMethod.Post;
        BucketName = args.BucketName;
        ObjectName = args.ObjectName ?? args.SourceObject.ObjectName;
        Headers = new Dictionary<string, string>(StringComparer.Ordinal);
        SSE = args.SSE;
        SSE?.Marshal(args.Headers);
        if (args.Headers?.Count > 0)
            Headers = Headers.Concat(args.Headers).GroupBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.First().Value, StringComparer.Ordinal);
    }

    internal string UploadId { get; set; }
    internal Dictionary<int, string> ETags { get; set; }

    internal override void Validate()
    {
        base.Validate();
        if (string.IsNullOrWhiteSpace(UploadId))
            throw new NullReferenceException(nameof(UploadId) + " cannot be empty.");
        if (ETags is null || ETags.Count <= 0)
            throw new InvalidOperationException(nameof(ETags) + " dictionary cannot be empty.");
    }

    internal CompleteMultipartUploadArgs WithUploadId(string uploadId)
    {
        UploadId = uploadId;
        return this;
    }

    internal CompleteMultipartUploadArgs WithETags(IDictionary<int, string> etags)
    {
        if (etags?.Count > 0) ETags = new Dictionary<int, string>(etags);
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploadId", $"{UploadId}");
        var parts = new List<XElement>();

        for (var i = 1; i <= ETags.Count; i++)
            parts.Add(new XElement("Part",
                new XElement("PartNumber", i),
                new XElement("ETag", ETags[i])));

        var completeMultipartUploadXml = new XElement("CompleteMultipartUpload", parts);
        var bodyString = completeMultipartUploadXml.ToString();
        ReadOnlyMemory<byte> bodyInBytes = Encoding.UTF8.GetBytes(bodyString);
        requestMessageBuilder.BodyParameters.Add("content-type", "application/xml");
        requestMessageBuilder.SetBody(bodyInBytes);
        // var bodyInCharArr = Encoding.UTF8.GetString(requestMessageBuilder.Content).ToCharArray();

        return requestMessageBuilder;
    }
}
