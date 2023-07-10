/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2021 MinIO, Inc.
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

using Minio.DataModel;

namespace Minio.Credentials;

public class AWSEnvironmentProvider : IClientProvider
{
    public AccessCredentials GetCredentials()
    {
        return new AccessCredentials(GetAccessKey(), GetSecretKey(), GetSessionToken(), default);
    }

    public ValueTask<AccessCredentials> GetCredentialsAsync()
    {
        return new ValueTask<AccessCredentials>(GetCredentials());
    }

    internal string GetAccessKey()
    {
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        if (string.IsNullOrWhiteSpace(accessKey)) accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
        return accessKey;
    }

    internal string GetSecretKey()
    {
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        if (string.IsNullOrWhiteSpace(secretKey)) secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");
        return secretKey;
    }

    internal string GetSessionToken()
    {
        return Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");
    }
}
