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

using System.Web;
using Minio.DataModel.Args;
using Minio.DataModel.Result;

namespace Minio.DataModel.Response;

internal class ListObjectVersionResponse
{
    internal Item BucketObjectsLastItem;
    internal IObserver<Item> ItemObservable;

    internal ListObjectVersionResponse(ListObjectsArgs args, Tuple<ListVersionsResult, List<Item>> objectList,
        IObserver<Item> obs)
    {
        ItemObservable = obs;
        foreach (var item in objectList.Item2)
        {
            BucketObjectsLastItem = item;
            if (string.Equals(objectList.Item1.EncodingType, "url", StringComparison.OrdinalIgnoreCase))
                item.Key = HttpUtility.UrlDecode(item.Key);
            ItemObservable.OnNext(item);
        }

        if (objectList.Item1.NextMarker is not null)
        {
            if (string.Equals(objectList.Item1.EncodingType, "url", StringComparison.OrdinalIgnoreCase))
            {
                NextMarker = HttpUtility.UrlDecode(objectList.Item1.NextMarker);
                NextKeyMarker = HttpUtility.UrlDecode(objectList.Item1.NextKeyMarker);
                NextVerMarker = HttpUtility.UrlDecode(objectList.Item1.NextVersionIdMarker);
            }
            else
            {
                NextMarker = objectList.Item1.NextMarker;
                NextKeyMarker = objectList.Item1.NextKeyMarker;
                NextVerMarker = objectList.Item1.NextVersionIdMarker;
            }
        }
        else if (BucketObjectsLastItem is not null)
        {
            if (string.Equals(objectList.Item1.EncodingType, "url", StringComparison.OrdinalIgnoreCase))
            {
                NextMarker = HttpUtility.UrlDecode(BucketObjectsLastItem.Key);
                NextKeyMarker = HttpUtility.UrlDecode(BucketObjectsLastItem.Key);
                NextVerMarker = HttpUtility.UrlDecode(BucketObjectsLastItem.VersionId);
            }
            else
            {
                NextMarker = BucketObjectsLastItem.Key;
                NextKeyMarker = BucketObjectsLastItem.Key;
                NextVerMarker = BucketObjectsLastItem.VersionId;
            }
        }
    }

    internal string NextMarker { get; }
    internal string NextKeyMarker { get; }
    internal string NextVerMarker { get; }
}
