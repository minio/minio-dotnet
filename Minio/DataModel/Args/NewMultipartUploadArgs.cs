﻿/*
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

using Minio.Helper;

namespace Minio.DataModel.Args;

internal class NewMultipartUploadArgs<T> : ObjectWriteArgs<T>
    where T : NewMultipartUploadArgs<T>
{
    internal NewMultipartUploadArgs()
    {
        RequestMethod = HttpMethod.Post;
    }

    internal DateTime RetentionUntilDate { get; set; }
    internal bool ObjectLockSet { get; set; }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploads", "");
        if (ObjectLockSet)
        {
            if (!RetentionUntilDate.Equals(default))
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                    Utils.To8601String(RetentionUntilDate));
        }

        requestMessageBuilder.AddOrUpdateHeaderParameter("content-type", ContentType);

        return requestMessageBuilder;
    }
}
