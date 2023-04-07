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

using System.Net;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
using Minio.DataModel;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Tags;

namespace Minio;

internal class SelectObjectContentResponse : GenericResponse
{
    internal SelectObjectContentResponse(HttpStatusCode statusCode, string responseContent,
        ReadOnlyMemory<byte> responseRawBytes)
        : base(statusCode, responseContent)
    {
        ResponseStream = new SelectResponseStream(responseRawBytes.AsStream());
    }

    internal SelectResponseStream ResponseStream { get; }
}

internal class StatObjectResponse : GenericResponse
{
    internal StatObjectResponse(HttpStatusCode statusCode, string responseContent,
        Dictionary<string, string> responseHeaders, StatObjectArgs args)
        : base(statusCode, responseContent)
    {
        // StatObjectResponse object is populated with available stats from the response.
        ObjectInfo = ObjectStat.FromResponseHeaders(args.ObjectName, responseHeaders);
    }

    internal ObjectStat ObjectInfo { get; set; }
}

internal class RemoveObjectsResponse : GenericResponse
{
    internal RemoveObjectsResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent));
        DeletedObjectsResult =
            (DeleteObjectsResult)new XmlSerializer(typeof(DeleteObjectsResult)).Deserialize(stream);
    }

    internal DeleteObjectsResult DeletedObjectsResult { get; }
}

internal class GetMultipartUploadsListResponse : GenericResponse
{
    internal GetMultipartUploadsListResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        ListMultipartUploadsResult uploadsResult = null;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            uploadsResult =
                (ListMultipartUploadsResult)new XmlSerializer(typeof(ListMultipartUploadsResult)).Deserialize(stream);
        }

        var root = XDocument.Parse(responseContent);
        var itemCheck = root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Upload").FirstOrDefault();
        if (uploadsResult == null || itemCheck?.HasElements != true) return;
        var uploads = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Upload")
            select new Upload
            {
                Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                UploadId = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}UploadId").Value,
                Initiated = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Initiated").Value
            };
        UploadResult = new Tuple<ListMultipartUploadsResult, List<Upload>>(uploadsResult, uploads.ToList());
    }

    internal Tuple<ListMultipartUploadsResult, List<Upload>> UploadResult { get; }
}

public class PresignedPostPolicyResponse
{
    public PresignedPostPolicyResponse(PresignedPostPolicyArgs args, Uri URI)
    {
        URIPolicyTuple = Tuple.Create(URI.AbsolutePath, args.Policy.FormData);
    }

    internal Tuple<string, Dictionary<string, string>> URIPolicyTuple { get; }
}

public class GetLegalHoldResponse : GenericResponse
{
    public GetLegalHoldResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) || !HttpStatusCode.OK.Equals(statusCode))
        {
            CurrentLegalHoldConfiguration = null;
            return;
        }

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            CurrentLegalHoldConfiguration =
                (ObjectLegalHoldConfiguration)new XmlSerializer(typeof(ObjectLegalHoldConfiguration)).Deserialize(
                    stream);
        }

        if (CurrentLegalHoldConfiguration == null
            || string.IsNullOrEmpty(CurrentLegalHoldConfiguration.Status))
            Status = "OFF";
        else
            Status = CurrentLegalHoldConfiguration.Status;
    }

    internal ObjectLegalHoldConfiguration CurrentLegalHoldConfiguration { get; }
    internal string Status { get; }
}

internal class GetObjectTagsResponse : GenericResponse
{
    public GetObjectTagsResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) ||
            !HttpStatusCode.OK.Equals(statusCode))
        {
            ObjectTags = null;
            return;
        }

        responseContent = Utils.RemoveNamespaceInXML(responseContent);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent));
        ObjectTags = (Tagging)new XmlSerializer(typeof(Tagging)).Deserialize(stream);
    }

    public Tagging ObjectTags { get; set; }
}

internal class GetRetentionResponse : GenericResponse
{
    public GetRetentionResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) && !HttpStatusCode.OK.Equals(statusCode))
        {
            CurrentRetentionConfiguration = null;
            return;
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent));
        CurrentRetentionConfiguration =
            (ObjectRetentionConfiguration)new XmlSerializer(typeof(ObjectRetentionConfiguration)).Deserialize(
                stream);
    }

    internal ObjectRetentionConfiguration CurrentRetentionConfiguration { get; }
}

internal class CopyObjectResponse : GenericResponse
{
    public CopyObjectResponse(HttpStatusCode statusCode, string content, Type reqType)
        : base(statusCode, content)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        if (reqType == typeof(CopyObjectResult))
            CopyObjectRequestResult =
                (CopyObjectResult)new XmlSerializer(typeof(CopyObjectResult)).Deserialize(stream);
        else
            CopyPartRequestResult = (CopyPartResult)new XmlSerializer(typeof(CopyPartResult)).Deserialize(stream);
    }

    internal CopyObjectResult CopyObjectRequestResult { get; set; }
    internal CopyPartResult CopyPartRequestResult { get; set; }
}

internal class NewMultipartUploadResponse : GenericResponse
{
    internal NewMultipartUploadResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        InitiateMultipartUploadResult newUpload = null;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            newUpload = (InitiateMultipartUploadResult)new XmlSerializer(typeof(InitiateMultipartUploadResult))
                .Deserialize(stream);
        }

        UploadId = newUpload.UploadId;
    }

    internal string UploadId { get; }
}

internal class PutObjectResponse : GenericResponse
{
    internal string Etag;

    internal PutObjectResponse(HttpStatusCode statusCode, string responseContent,
        Dictionary<string, string> responseHeaders)
        : base(statusCode, responseContent)
    {
        if (responseHeaders.ContainsKey("Etag"))
        {
            if (!string.IsNullOrEmpty("Etag"))
                Etag = responseHeaders["ETag"];
            return;
        }

        foreach (var parameter in responseHeaders)
            if (parameter.Key.Equals("ETag", StringComparison.OrdinalIgnoreCase))
            {
                Etag = parameter.Value;
                return;
            }
    }
}