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
        }    }
}