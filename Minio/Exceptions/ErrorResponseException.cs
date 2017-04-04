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

using RestSharp;

namespace Minio.Exceptions
{
    public class ErrorResponseException : MinioException
    {
        private string ErrorCode;

        public ErrorResponseException(IRestResponse response)
            : base($"Minio API responded with status code={response.StatusCode}, response={response.ErrorMessage}, content={response.Content}")
        {
            this.response = response;
        }
        public ErrorResponseException()
        {

        }
        public ErrorResponseException(string message,string errorcode) : base($"Minio API responded with message={message}")
        {
            this.message = message;
            this.ErrorCode = errorcode;
        }

        public override string ToString()
        {
            return this.message + ": " + base.ToString();
        }
    }
}
