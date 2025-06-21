/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2017, 2018, 2019, 2020 MinIO, Inc.
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

using Minio.DataModel.Result;

namespace Minio.Exceptions;

[Serializable]
public class BucketNotFoundException : MinioException
{
    private readonly string bucketName;

    public BucketNotFoundException(string bucketName, string message = "Bucket NotFound") : base(message)
    {
        this.bucketName = bucketName;
    }

    public BucketNotFoundException(ResponseResult serverResponse) : base(serverResponse)
    {
    }

    public BucketNotFoundException(string message = "Bucket NotFound") : base(message)
    {
    }

    public BucketNotFoundException()
    {
    }

    public BucketNotFoundException(Exception innerException, string message = "Bucket NotFound") : base(message,
        innerException)
    {
    }

    public BucketNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public BucketNotFoundException(string message, ResponseResult serverResponse) : base(message, serverResponse)
    {
    }

    public override string ToString()
    {
        return $"{bucketName}: {base.ToString()}";
    }
}
