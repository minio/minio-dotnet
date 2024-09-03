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

public class GetPresignedUrlArgs : ObjectArgs<GetPresignedUrlArgs>
{
    public GetPresignedUrlArgs(PresignedUrlHttpMethod requestMethod)
    {
        RequestMethod = ToHttpMethod(requestMethod);
    }

    internal int Expiry { get; set; }
    internal DateTime? RequestDate { get; set; }

    internal override void Validate()
    {
        base.Validate();
        if (!Utils.IsValidExpiry(Expiry))
            throw new InvalidExpiryRangeException("Expiry range should be between 1 and " +
                                                  Constants.DefaultExpiryTime);
    }

    public GetPresignedUrlArgs WithExpiry(int expiry)
    {
        Expiry = expiry;
        return this;
    }

    public GetPresignedUrlArgs WithRequestDate(DateTime? d)
    {
        RequestDate = d;
        return this;
    }
    
    public enum PresignedUrlHttpMethod
    {
        Get,
        Put,
        Delete
    }
    
    private static HttpMethod ToHttpMethod(PresignedUrlHttpMethod method)
    {
        return method switch
        {
            PresignedUrlHttpMethod.Get => HttpMethod.Get,
            PresignedUrlHttpMethod.Put => HttpMethod.Put,
            PresignedUrlHttpMethod.Delete => HttpMethod.Delete,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };
    }
}
