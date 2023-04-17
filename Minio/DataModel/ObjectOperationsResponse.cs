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
        using var stream = responseRawBytes.AsStream();
        ResponseStream = new SelectResponseStream(stream);
    }

    internal SelectResponseStream ResponseStream { get; }
}

internal class StatObjectResponse : GenericResponse
{
    internal StatObjectResponse(HttpStatusCode statusCode, string responseContent,
        IDictionary<string, string> responseHeaders, StatObjectArgs args)
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
        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        DeletedObjectsResult = Utils.DeserializeXml<DeleteObjectsResult>(stream);
    }

    internal DeleteObjectsResult DeletedObjectsResult { get; }
}

internal class GetMultipartUploadsListResponse : GenericResponse
{
    internal GetMultipartUploadsListResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        var uploadsResult = Utils.DeserializeXml<ListMultipartUploadsResult>(stream);
        var root = XDocument.Parse(responseContent);
        XNamespace ns = Utils.DetermineNamespace(root);

        var itemCheck = root.Root.Descendants(ns + "Upload").FirstOrDefault();
        if (uploadsResult == null || itemCheck?.HasElements != true) return;
        var uploads = from c in root.Root.Descendants(ns + "Upload")
            select new Upload
            {
                Key = c.Element(ns + "Key").Value,
                UploadId = c.Element(ns + "UploadId").Value,
                Initiated = c.Element(ns + "Initiated").Value
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

    internal Tuple<string, IDictionary<string, string>> URIPolicyTuple { get; }
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

        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        CurrentLegalHoldConfiguration =
            Utils.DeserializeXml<ObjectLegalHoldConfiguration>(stream);

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
        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        ObjectTags = Utils.DeserializeXml<Tagging>(stream);
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

        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        CurrentRetentionConfiguration =
            Utils.DeserializeXml<ObjectRetentionConfiguration>(stream);
    }

    internal ObjectRetentionConfiguration CurrentRetentionConfiguration { get; }
}

internal class CopyObjectResponse : GenericResponse
{
    public CopyObjectResponse(HttpStatusCode statusCode, string responseContent, Type reqType)
        : base(statusCode, responseContent)
    {
        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        if (reqType == typeof(CopyObjectResult))
            CopyObjectRequestResult = Utils.DeserializeXml<CopyObjectResult>(stream);
        else
            CopyPartRequestResult = Utils.DeserializeXml<CopyPartResult>(stream);
    }

    internal CopyObjectResult CopyObjectRequestResult { get; set; }
    internal CopyPartResult CopyPartRequestResult { get; set; }
}

internal class NewMultipartUploadResponse : GenericResponse
{
    internal NewMultipartUploadResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        var newUpload = Utils.DeserializeXml<InitiateMultipartUploadResult>(stream);

        UploadId = newUpload.UploadId;
    }

    internal string UploadId { get; }
}

public class PutObjectResponse : GenericResponse
{
    public string Etag;
    public string ObjectName;
    public long Size;

    public PutObjectResponse(HttpStatusCode statusCode, string responseContent,
        IDictionary<string, string> responseHeaders, long size, string name)
        : base(statusCode, responseContent)
    {
        foreach (var parameter in responseHeaders)
            if (parameter.Key.Equals("ETag", StringComparison.OrdinalIgnoreCase))
            {
                Etag = parameter.Value;
                break;
            }

        Size = size;
        ObjectName = name;
    }
}