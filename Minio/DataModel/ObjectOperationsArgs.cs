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
using System.IO;
using RestSharp;

using Minio.DataModel;
using Minio.Exceptions;

namespace Minio
{
    public class SelectObjectContentArgs: ObjectArgs<SelectObjectContentArgs>
    {
        internal SelectObjectOptions SelectOptions { get; private set; }

        public SelectObjectContentArgs()
        {
            this.RequestMethod = Method.POST;
        }

        public SelectObjectContentArgs WithSelectObjectOptions(SelectObjectOptions opts)
        {
            this.SelectOptions = opts;
            if (opts.SSE != null)
            {
                Dictionary<string,string> sseHeaders = new Dictionary<string,string>();
                opts.SSE.Marshal(sseHeaders);
                this.WithHeaders(sseHeaders);
            }
            return this;
        }

        public override void Validate()
        {
            base.Validate();
            if (SelectOptions == null)
            {
                throw new ArgumentException("Options cannot be null", nameof(SelectOptions));
            }
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            var selectReqBytes = System.Text.Encoding.UTF8.GetBytes(this.SelectOptions.MarshalXML());
            request.AddQueryParameter("select","");
            request.AddQueryParameter("select-type","2");
            request.AddParameter("application/xml", selectReqBytes, ParameterType.RequestBody);
            return request;
        }
    }
}