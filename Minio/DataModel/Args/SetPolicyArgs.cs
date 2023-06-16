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

namespace Minio.DataModel.Args;

public class SetPolicyArgs : BucketArgs<SetPolicyArgs>
{
    public SetPolicyArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal string PolicyJsonString { get; private set; }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (string.IsNullOrEmpty(PolicyJsonString))
            throw new MinioException("SetPolicyArgs needs the policy to be set to the right JSON contents.");

        requestMessageBuilder.AddQueryParameter("policy", "");
        requestMessageBuilder.AddJsonBody(PolicyJsonString);
        return requestMessageBuilder;
    }

    public SetPolicyArgs WithPolicy(string policy)
    {
        PolicyJsonString = policy;
        return this;
    }
}