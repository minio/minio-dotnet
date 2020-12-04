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

    public class StatObjectArgs : ObjectQueryArgs<StatObjectArgs>
    {
        public StatObjectArgs()
        {
            this.RequestMethod = Method.HEAD;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            if (!string.IsNullOrEmpty(this.MatchETag))
            {
                request.AddOrUpdateParameter("If-Match", this.MatchETag, ParameterType.HttpHeader);
            }
            if (!string.IsNullOrEmpty(this.NotMatchETag))
            {
                request.AddOrUpdateParameter("If-None-Match", this.NotMatchETag, ParameterType.HttpHeader);
            }
            if (this.ModifiedSince != null && this.ModifiedSince != default(DateTime))
            {
                request.AddOrUpdateParameter("If-Modified-Since", this.ModifiedSince, ParameterType.HttpHeader);
            }
            if(this.UnModifiedSince != null && this.UnModifiedSince != default(DateTime))
            {
                request.AddOrUpdateParameter("If-Unmodified-Since", this.UnModifiedSince, ParameterType.HttpHeader);
            }
            
            return request;
        }

        public override void Validate()
        {
            base.Validate();
            if (!string.IsNullOrEmpty(this.NotMatchETag) && !string.IsNullOrEmpty(this.MatchETag))
            {
                throw new InvalidOperationException("Invalid to set both Etag match conditions " + nameof(this.NotMatchETag) + " and " + nameof(this.MatchETag));
            }
            if ((this.ModifiedSince != null && !this.ModifiedSince.Equals(default(DateTime))) &&
                    (this.ModifiedSince != null && !this.UnModifiedSince.Equals(default(DateTime))))
            {
                throw new InvalidOperationException("Invalid to set both modified date match conditions " + nameof(this.ModifiedSince) + " and " + nameof(this.UnModifiedSince));
            }
        }
    }

    public class PresignedPostPolicyArgs : ObjectArgs<PresignedPostPolicyArgs>
    {
        internal PostPolicy Policy { get; set; }
        internal DateTime Expiration { get; set; }

        internal string Region { get; set; }
        public new void Validate()
        {
            bool checkPolicy = false;
            try
            {
                utils.ValidateBucketName(this.BucketName);
                utils.ValidateObjectName(this.ObjectName);
            }
            catch (Exception ex)
            {
                if (ex is InvalidBucketNameException || ex is InvalidObjectNameException)
                {
                    checkPolicy = true;
                }
                else
                {
                    throw;
                }
            }
            if (checkPolicy)
            {
                if (!this.Policy.IsBucketSet())
                {
                    throw new InvalidOperationException("For the " + nameof(Policy) + " bucket should be set");
                }

                if (!this.Policy.IsKeySet())
                {
                    throw new InvalidOperationException("For the " + nameof(Policy) + " key should be set");
                }

                if (!this.Policy.IsExpirationSet())
                {
                    throw new InvalidOperationException("For the " + nameof(Policy) + " expiration should be set");
                }
                this.BucketName = this.Policy.Bucket;
                this.ObjectName = this.Policy.Key;
            }
            if (string.IsNullOrEmpty(this.Expiration.ToString()))
            {
                throw new InvalidOperationException("For the " + nameof(Policy) + " expiration should be set");
            }

        }

        public PresignedPostPolicyArgs WithExpiration(DateTime ex)
        {
            this.Expiration = ex;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            return request;
        }

        internal PresignedPostPolicyArgs WithRegion(string region)
        {
            this.Region = region;
            return this;
        }

        internal PresignedPostPolicyArgs WithSessionToken(string sessionToken)
        {
            this.Policy.SetSessionToken(sessionToken);
            return this;
        }

        internal PresignedPostPolicyArgs WithCredential(string credential)
        {
            this.Policy.SetCredential(credential);
            return this;
        }

        internal PresignedPostPolicyArgs WithSignature(string signature)
        {
            this.Policy.SetSignature(signature);
            return this;
        }
        public PresignedPostPolicyArgs WithPolicy(PostPolicy policy)
        {
            this.Policy = policy;
            return this;
        }
    }

    public class PresignedPutObjectArgs : ObjectArgs<PresignedPutObjectArgs>
    {
        internal int Expiry { get; set; }

        public PresignedPutObjectArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        internal new void Validate()
        {
            base.Validate();
            if (!utils.IsValidExpiry(this.Expiry))
            {
                throw new InvalidExpiryRangeException("Expiry range should be between 1 seconds and " + Constants.DefaultExpiryTime.ToString() + " seconds");
            }
        }

        public PresignedPutObjectArgs WithExpiry(int ex)
        {
            this.Expiry = ex;
            return this;
        }
    }

    public class RemoveUploadArgs : EncryptionArgs<RemoveUploadArgs>
    {
        internal string UploadId { get; private set; }
        public RemoveUploadArgs()
        {
            this.RequestMethod = Method.DELETE;
        }

        public RemoveUploadArgs WithUploadId(string id)
        {
            this.UploadId = id;
            return this;
        }

        public override void Validate()
        {
            base.Validate();
            if(string.IsNullOrEmpty(this.UploadId))
            {
                throw new InvalidOperationException(nameof(UploadId) + " cannot be empty. Please assign a valid upload ID to remove.");
            }
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request.AddQueryParameter("uploadId",$"{this.UploadId}");
            return request;
        }
    }

    public class RemoveIncompleteUploadArgs : EncryptionArgs<RemoveIncompleteUploadArgs>
    {
        public RemoveIncompleteUploadArgs()
        {
            this.RequestMethod = Method.DELETE;
        }
    }
}