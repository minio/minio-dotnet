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
using Minio.DataModel.Result;
using Minio.Helper;

namespace Minio.DataModel.Response;

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
        if (uploadsResult is null || itemCheck?.HasElements != true) return;
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
