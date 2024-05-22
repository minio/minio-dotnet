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
using System.Xml.Linq;
using Minio.DataModel.Result;
using Minio.Helper;

namespace Minio.DataModel.Response;

internal class GetObjectsVersionsListResponse : GenericResponse
{
    internal ListVersionsResult BucketResult;
    internal Tuple<ListVersionsResult, List<Item>> ObjectsTuple;

    internal GetObjectsVersionsListResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (HttpStatusCode.OK != statusCode) return;

        BucketResult = Utils.DeserializeXml<ListVersionsResult>(responseContent);

        List<Item> items = [];
        var root = XDocument.Parse(responseContent);
        XNamespace ns = Utils.DetermineNamespace(root);

        var versNodes = root.Root.Descendants(ns + "Version");
        var userMtdt = versNodes.Descendants(ns + "UserMetadata");

        for (var indx = 0; versNodes.Skip(indx).Any(); indx++)
        {
            var item = new Item
            {
                Key = versNodes.ToList()[indx].Element(ns + "Key").Value,
                LastModified = versNodes.ToList()[indx].Element(ns + "LastModified").Value,
                ETag = versNodes.ToList()[indx].Element(ns + "ETag").Value,
                VersionId = versNodes.ToList()[indx].Element(ns + "VersionId").Value,
                StorageClass = versNodes.ToList()[indx].Element(ns + "StorageClass").Value,
                Size = ulong.Parse(versNodes.ToList()[indx].Element(ns + "Size").Value),
                IsLatest = bool.Parse(versNodes.ToList()[indx].Element(ns + "IsLatest").Value),
                IsDir = BucketResult.Prefix is not null
            };

            if (userMtdt.Any())
            {
                List<MetadataItem> userMetadata = [];
                foreach (var el in userMtdt.ToList()[indx].Elements())
                {
                    var strippedEl = Utils.RemoveAllNamespaces(el);
                    if (strippedEl.Name.ToString().StartsWith("X-Amz-Meta-", StringComparison.Ordinal))
                        userMetadata.Add(new MetadataItem(strippedEl.Name.ToString(), (string)strippedEl));
                }

                if (userMetadata.Count > 0)
                    item.UserMetadata = userMetadata;
            }

            items.Add(item);
        }
        ObjectsTuple = new Tuple<ListVersionsResult, List<Item>>(BucketResult, items);
    }
}
