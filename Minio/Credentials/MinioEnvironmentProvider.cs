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

namespace Minio.Credentials
{
    public class MinioEnvironmentProvider : IClientProvider
    {
        public AccessCredentials GetCredentials()
        {
            var accessKey = Environment.GetEnvironmentVariable("MINIO_ROOT_USER");
            var secretKey = Environment.GetEnvironmentVariable("MINIO_ROOT_PASSWORD");

            if (string.IsNullOrEmpty(accessKey))
            {
                accessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
            }

            if (string.IsNullOrEmpty(secretKey))
            {
                secretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");
            }

            return new AccessCredentials(accessKey, secretKey, null, default);
        }

        public ValueTask<AccessCredentials> GetCredentialsAsync()
        {
            return new ValueTask<AccessCredentials>(GetCredentials());
        }
    }
}