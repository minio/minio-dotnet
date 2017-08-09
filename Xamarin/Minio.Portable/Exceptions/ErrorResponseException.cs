/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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

namespace Minio.Exceptions
{
    using RestSharp.Portable;

    public class ErrorResponseException : MinioException
    {
        public ErrorResponseException(IRestResponse restResponse)
            : base(
                $"Minio API responded with status code={restResponse.StatusCode}, response={restResponse.StatusDescription}, content={restResponse.Content}")
        {
            this.RestResponse = restResponse;
        }

        public ErrorResponseException()
        {
        }

        public ErrorResponseException(string message, string errorcode) : base(
            $"Minio API responded with message={message}")
        {
            this.Message = message;
            this.ErrorCode = errorcode;
        }

        public override string Message { get; }

        public string ErrorCode { get; }

        public override string ToString()
        {
            return this.Message + ": " + base.ToString();
        }
    }
}