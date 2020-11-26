/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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
using System.Globalization;
using System.IO;
using System.Net;
using RestSharp;

using Minio.DataModel;

namespace Minio
{
    internal class SelectObjectContentResponse : GenericResponse
    {
        internal SelectResponseStream ResponseStream { get; private set; }
        internal SelectObjectContentResponse(HttpStatusCode statusCode, string responseContent, byte[] responseRawBytes)
                    : base(statusCode, responseContent)
        {
            this.ResponseStream = new SelectResponseStream(new MemoryStream(responseRawBytes));
        }

    }

    internal class StatObjectResponse : GenericResponse
    {
        internal ObjectStat ObjectInfo { get; set; }
        internal StatObjectResponse(HttpStatusCode statusCode, string responseContent, IList<Parameter> responseHeaders, StatObjectArgs args)
                    : base(statusCode, responseContent)
        {
            // StatObjectResponse object is populated with available stats from the response.
            this.ObjectInfo = ObjectStat.FromResponseHeaders(args.ObjectName, responseHeaders);
        }
    }

    internal class PresignedPostPolicyResponse
    {
        internal Tuple<string, Dictionary<string, string>> URIPolicyTuple { get; private set; }

        public PresignedPostPolicyResponse(PresignedPostPolicyArgs args, string absURI)
        {
            args.Policy.SetAlgorithm("AWS4-HMAC-SHA256");
            args.Policy.SetDate(DateTime.UtcNow);
            args.Policy.SetPolicy(args.Policy.Base64());
            URIPolicyTuple = Tuple.Create(absURI, args.Policy.GetFormData());
        }
    }

}
