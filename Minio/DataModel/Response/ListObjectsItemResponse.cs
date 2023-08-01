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

internal class ListObjectsItemResponse
{
    internal Item BucketObjectsLastItem;
    internal IObserver<Item> ItemObservable;

    internal ListObjectsItemResponse(ListObjectsArgs args, Tuple<ListBucketResult, List<Item>> objectList,
        IObserver<Item> obs)
    {
        ItemObservable = obs;
        NextMarker = string.Empty;
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
                NextMarker = HttpUtility.UrlDecode(objectList.Item1.NextMarker);
            else
                NextMarker = objectList.Item1.NextMarker;
        }
        else if (BucketObjectsLastItem is not null)
        {
            if (string.Equals(objectList.Item1.EncodingType, "url", StringComparison.OrdinalIgnoreCase))
                NextMarker = HttpUtility.UrlDecode(BucketObjectsLastItem.Key);
            else
                NextMarker = BucketObjectsLastItem.Key;
        }
    }

    internal string NextMarker { get; }
}
