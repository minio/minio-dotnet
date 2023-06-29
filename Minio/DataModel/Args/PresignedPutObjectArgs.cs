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

using Minio.Exceptions;
using Minio.Helper;

namespace Minio.DataModel.Args;

public class PresignedPutObjectArgs : ObjectArgs<PresignedPutObjectArgs>
{
    public PresignedPutObjectArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal int Expiry { get; set; }

    protected new void Validate()
    {
        base.Validate();
        if (!Utils.IsValidExpiry(Expiry))
            throw new InvalidExpiryRangeException("Expiry range should be between 1 seconds and " +
                                                  Constants.DefaultExpiryTime + " seconds");
    }

    public PresignedPutObjectArgs WithExpiry(int ex)
    {
        Expiry = ex;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        return requestMessageBuilder;
    }
}
