﻿/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2015 Minio, Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio.Exceptions;
using System.Net;

namespace Minio
{
    public partial class ClientApiOperations
    {
        internal MinioClient client;
        private const string RegistryAuthHeaderKey = "X-Registry-Auth";
        internal static readonly ApiResponseErrorHandlingDelegate NoSuchBucketHandler = (response) =>
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new BucketNotFoundException();
            }
        };
        public ClientApiOperations(MinioClient client)
        {
            this.client = client;
        }
    }
}

