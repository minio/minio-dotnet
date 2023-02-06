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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Minio.DataModel;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Tags;

namespace Minio;

internal class SelectObjectContentResponse : GenericResponse
{
    internal SelectObjectContentResponse(ResponseResult result)
        : base(result)
    {
        ResponseStream = new SelectResponseStream(new MemoryStream(result.ContentBytes));
    }

    internal SelectResponseStream ResponseStream { get; }
}

internal class StatObjectResponse : GenericResponse
{
    internal StatObjectResponse(ResponseResult result, StatObjectArgs args)
        : base(result)
    {
        // StatObjectResponse object is populated with available stats from the response.
        ObjectInfo = ObjectStat.FromResponseHeaders(args.ObjectName, result.Headers);
    }

    internal ObjectStat ObjectInfo { get; set; }
}

internal class RemoveObjectsResponse : GenericXmlResponse<DeleteObjectsResult>
{
    internal RemoveObjectsResponse(ResponseResult result)
        : base(result)
    {
    }

    internal DeleteObjectsResult DeletedObjectsResult => _result;
}

internal class GetMultipartUploadsListResponse : GenericResponse
{
    internal GetMultipartUploadsListResponse(ResponseResult result)
        : base(result)
    {
        ListMultipartUploadsResult uploadsResult = null;
        using (var stream = new MemoryStream(result.ContentBytes))
        {
            uploadsResult =
                (ListMultipartUploadsResult)new XmlSerializer(typeof(ListMultipartUploadsResult)).Deserialize(stream);
        }

        var root = XDocument.Parse(result.Content);
        var itemCheck = root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Upload").FirstOrDefault();
        if (uploadsResult == null || itemCheck == null || !itemCheck.HasElements) return;
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
        URIPolicyTuple = Tuple.Create(URI.AbsolutePath, args.Policy.GetFormData());
    }

    internal Tuple<string, Dictionary<string, string>> URIPolicyTuple { get; }
}

public class GetLegalHoldResponse : GenericXmlResponse<ObjectLegalHoldConfiguration>
{
    public GetLegalHoldResponse(ResponseResult result)
        : base(result)
    {
    }

    internal ObjectLegalHoldConfiguration CurrentLegalHoldConfiguration => _result;
    internal string Status => string.IsNullOrEmpty(CurrentLegalHoldConfiguration?.Status) 
        ? "OFF" 
        : CurrentLegalHoldConfiguration.Status;
}

internal class GetObjectTagsResponse : GenericXmlResponse<Tagging>
{
    public GetObjectTagsResponse(ResponseResult result)
        : base(result)
    {
    }

    protected override string ConvertContent(string content)
    {
        return utils.RemoveNamespaceInXML(content);
    }

    public Tagging ObjectTags => _result;
}

internal class GetRetentionResponse : GenericXmlResponse<ObjectRetentionConfiguration>
{
    public GetRetentionResponse(ResponseResult result)
        : base(result)
    {
    }

    internal ObjectRetentionConfiguration CurrentRetentionConfiguration => _result;
}

internal class CopyObjectResponse : GenericResponse
{
    public CopyObjectResponse(ResponseResult result, Type reqType)
        : base(result)
    {
        using (var stream = new MemoryStream(result.ContentBytes))
        {
            if (reqType == typeof(CopyObjectResult))
                CopyObjectRequestResult =
                    (CopyObjectResult)new XmlSerializer(typeof(CopyObjectResult)).Deserialize(stream);
            else
                CopyPartRequestResult = (CopyPartResult)new XmlSerializer(typeof(CopyPartResult)).Deserialize(stream);
        }
    }

    internal CopyObjectResult CopyObjectRequestResult { get; set; }
    internal CopyPartResult CopyPartRequestResult { get; set; }
}

internal class NewMultipartUploadResponse : GenericXmlResponse<InitiateMultipartUploadResult>
{
    internal NewMultipartUploadResponse(ResponseResult result)
        : base(result)
    {
    }

    internal string UploadId => _result?.UploadId;
}

internal class PutObjectResponse : GenericResponse
{
    internal string Etag => Headers.GetValueOrNull("Etag");

    internal PutObjectResponse(ResponseResult result)
        : base(result)
    {
    }
}
