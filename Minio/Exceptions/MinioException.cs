﻿/*
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

using System;

using RestSharp;

namespace Minio.Exceptions
{
    [Serializable]
    public class MinioException : Exception
    {
        private static string GetMessage(string message, IRestResponse serverResponse)
        {
            if (serverResponse == null && string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));

            if (serverResponse == null)
                return $"MinIO API responded with message={message}";

            if (message == null)
                return $"MinIO API responded with status code={serverResponse.StatusCode}, response={serverResponse.ErrorMessage}, content={serverResponse.Content}";

            return $"MinIO API responded with message={message}. Status code={serverResponse.StatusCode}, response={serverResponse.ErrorMessage}, content={serverResponse.Content}";
        }

        public MinioException(IRestResponse serverResponse)
            : this(null, serverResponse)
        {
        }

        public MinioException(string message)
            : this(message, (IRestResponse)null)
        {
        }

        public MinioException(string message, Exception exception)
            : base(message, exception)
        {
        }

        public MinioException(string message, IRestResponse serverResponse)
            : base(GetMessage(message, serverResponse))
        {
            this.ServerMessage = message;
            this.ServerResponse = serverResponse;

        }

        public string ServerMessage { get; }

        public IRestResponse ServerResponse { get; }

        public ErrorResponse Response { get; internal set; }

        public string XmlError { get; internal set; }
    }
}
