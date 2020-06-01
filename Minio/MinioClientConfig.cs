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

namespace Minio
{
    /// <summary>
    /// Configuration parameters for <see cref="MinioClient"/>
    /// </summary>
    public class MinioClientConfig
    {
        /// <summary>
        /// Location of the server, supports HTTP and HTTPS
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Connect to Cloud Storage with HTTPS
        /// </summary>
        public bool Secure { get; set; } = false;

        /// <summary>
        /// Access Key for authenticated requests (Optional, can be omitted for anonymous requests)
        /// </summary>
        public string AccessKey { get; set; } = "";

        /// <summary>
        /// Secret Key for authenticated requests (Optional, can be omitted for anonymous requests)
        /// </summary>
        public string SecretKey { get; set; } = "";

        /// <summary>
        /// Optional custom region
        /// </summary>
        public string Region { get; set; } = "";

        /// <summary>
        /// Optional session token
        /// </summary>
        public string SessionToken { get; set; } = "";

        /// <summary>
        /// Change the default for multipart part sizes when transferring between servers
        /// </summary>
        public long DefaultServerTransferPartSize { get; set; } = 512 * 1024L * 1024L;
    }
}
