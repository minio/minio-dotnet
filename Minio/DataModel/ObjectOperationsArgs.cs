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
using RestSharp;

using Minio.DataModel;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio
{
    public class SelectObjectContentArgs: EncryptionArgs<SelectObjectContentArgs>
    {
        private SelectObjectOptions SelectOptions;

        public SelectObjectContentArgs()
        {
            this.RequestMethod = Method.POST;
            this.SelectOptions = new SelectObjectOptions();
        }

        public override void Validate()
        {
            base.Validate();
            if (string.IsNullOrEmpty(this.SelectOptions.Expression))
            {
                throw new InvalidOperationException("The Expression " + nameof(this.SelectOptions.Expression) + " for Select Object Content cannot be empty.");
            }
            if ((this.SelectOptions.InputSerialization == null) || (this.SelectOptions.OutputSerialization == null))
            {
                throw new InvalidOperationException("The Input/Output serialization members for SelectObjectContentArgs should be initialized " + nameof(this.SelectOptions.InputSerialization) + " " + nameof(this.SelectOptions.OutputSerialization));
            }
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            if (this.RequestBody == null)
            {
                this.RequestBody = System.Text.Encoding.UTF8.GetBytes(this.SelectOptions.MarshalXML());
            }
            request.AddQueryParameter("select","");
            request.AddQueryParameter("select-type","2");
            request.AddParameter("application/xml", (byte[])this.RequestBody, ParameterType.RequestBody);
            return request;
        }

        public SelectObjectContentArgs WithExpressionType(QueryExpressionType e)
        {
            this.SelectOptions.ExpressionType = e;
            return this;
        }

        public SelectObjectContentArgs WithQueryExpression(string expr)
        {
            this.SelectOptions.Expression = expr;
            return this;
        }

        public SelectObjectContentArgs WithInputSerialization(SelectObjectInputSerialization serialization)
        {
            this.SelectOptions.InputSerialization = serialization;
            return this;
        }

        public SelectObjectContentArgs WithOutputSerialization(SelectObjectOutputSerialization serialization)
        {
            this.SelectOptions.OutputSerialization = serialization;
            return this;
        }

        public SelectObjectContentArgs WithRequestProgress(RequestProgress requestProgress)
        {
            this.SelectOptions.RequestProgress = requestProgress;
            return this;
        }
    }

    public class ListIncompleteUploadsArgs : BucketArgs<ListIncompleteUploadsArgs>
    {
        internal string Prefix { get; private set; }
        internal string Delimiter { get; private set; }
        internal bool Recursive { get; private set; }
        public ListIncompleteUploadsArgs()
        {
            this.RequestMethod = Method.GET;
            this.Recursive = true;
        }
        public ListIncompleteUploadsArgs WithPrefix(string prefix)
        {
            this.Prefix = prefix ?? string.Empty;
            return this;
        }

        public ListIncompleteUploadsArgs WithDelimiter(string delim)
        {
            this.Delimiter = delim ?? string.Empty;
            return this;
        }

        public ListIncompleteUploadsArgs WithRecursive(bool recursive)
        {
            this.Recursive = recursive;
            this.Delimiter = (recursive)? string.Empty : "/";
            return this;
        }
    }

    public class GetMultipartUploadsListArgs : BucketArgs<GetMultipartUploadsListArgs>
    {
        internal string Prefix { get; private set; }
        internal string Delimiter { get; private set; }
        internal string KeyMarker { get; private set; }
        internal string UploadIdMarker { get; private set; }
        internal uint MAX_UPLOAD_COUNT { get; private set; }
        public GetMultipartUploadsListArgs()
        {
            this.RequestMethod = Method.GET;
            this.MAX_UPLOAD_COUNT = 1000;
        }
        public GetMultipartUploadsListArgs WithPrefix(string prefix)
        {
            this.Prefix = prefix ?? string.Empty;
            return this;
        }

        public GetMultipartUploadsListArgs WithDelimiter(string delim)
        {
            this.Delimiter = delim ?? string.Empty;
            return this;
        }

        public GetMultipartUploadsListArgs WithKeyMarker(string nextKeyMarker)
        {
            this.KeyMarker = nextKeyMarker ?? string.Empty;
            return this;
        }

        public GetMultipartUploadsListArgs WithUploadIdMarker(string nextUploadIdMarker)
        {
            this.UploadIdMarker = nextUploadIdMarker ?? string.Empty;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request.AddQueryParameter("uploads","");
            request.AddQueryParameter("prefix",this.Prefix);
            request.AddQueryParameter("delimiter",this.Delimiter);
            request.AddQueryParameter("key-marker",this.KeyMarker);
            request.AddQueryParameter("upload-id-marker",this.UploadIdMarker);
            request.AddQueryParameter("max-uploads",this.MAX_UPLOAD_COUNT.ToString());
            return request;
        }
    }

    public class PresignedGetObjectArgs : ObjectArgs<PresignedGetObjectArgs>
    {
        internal int Expiry { get; set; }
        internal DateTime? RequestDate { get; set; }

        public PresignedGetObjectArgs()
        {
            this.RequestMethod = Method.GET;
        }

        public override void Validate()
        {
            base.Validate();
            if (!utils.IsValidExpiry(this.Expiry))
            {
                throw new InvalidExpiryRangeException("expiry range should be between 1 and " + Constants.DefaultExpiryTime.ToString());
            }
        }

        public PresignedGetObjectArgs WithExpiry(int expiry)
        {
            this.Expiry = expiry;
            return this;
        }

        public PresignedGetObjectArgs WithRequestDate(DateTime? d)
        {
            this.RequestDate = d;
            return this;
        }
    }
}