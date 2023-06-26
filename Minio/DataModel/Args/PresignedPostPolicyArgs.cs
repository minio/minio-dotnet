/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020, 2021 MinIO, Inc.
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

using System.Globalization;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio.DataModel.Args
{
    public class PresignedPostPolicyArgs : ObjectArgs<PresignedPostPolicyArgs>
    {
        internal PostPolicy Policy { get; set; }
        internal DateTime Expiration { get; set; }

        internal string Region { get; set; }

        protected new void Validate()
        {
            var checkPolicy = false;
            try
            {
                Utils.ValidateBucketName(BucketName);
                Utils.ValidateObjectName(ObjectName);
            }
            catch (Exception ex) when (ex is InvalidBucketNameException || ex is InvalidObjectNameException)
            {
                checkPolicy = true;
            }

            if (checkPolicy)
            {
                if (!Policy.IsBucketSet())
                {
                    throw new InvalidOperationException("For the " + nameof(Policy) + " bucket should be set");
                }

                if (!Policy.IsKeySet())
                {
                    throw new InvalidOperationException("For the " + nameof(Policy) + " key should be set");
                }

                if (!Policy.IsExpirationSet())
                {
                    throw new InvalidOperationException("For the " + nameof(Policy) + " expiration should be set");
                }

                BucketName = Policy.Bucket;
                ObjectName = Policy.Key;
            }

            if (string.IsNullOrEmpty(Expiration.ToString(CultureInfo.InvariantCulture)))
            {
                throw new InvalidOperationException("For the " + nameof(Policy) + " expiration should be set");
            }
        }

        public PresignedPostPolicyArgs WithExpiration(DateTime ex)
        {
            Expiration = ex;
            return this;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {
            return requestMessageBuilder;
        }

        internal PresignedPostPolicyArgs WithRegion(string region)
        {
            Region = region;
            return this;
        }

        internal PresignedPostPolicyArgs WithSessionToken(string sessionToken)
        {
            Policy.SetSessionToken(sessionToken);
            return this;
        }

        internal PresignedPostPolicyArgs WithDate(DateTime date)
        {
            Policy.SetDate(date);
            return this;
        }

        internal PresignedPostPolicyArgs WithCredential(string credential)
        {
            Policy.SetCredential(credential);
            return this;
        }

        internal PresignedPostPolicyArgs WithAlgorithm(string algorithm)
        {
            Policy.SetAlgorithm(algorithm);
            return this;
        }

        internal PresignedPostPolicyArgs WithSignature(string signature)
        {
            Policy.SetSignature(signature);
            return this;
        }

        public PresignedPostPolicyArgs WithPolicy(PostPolicy policy)
        {
            if (policy is null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            Policy = policy;
            if (policy.Expiration != DateTime.MinValue)
                // policy.expiration has an assigned value
            {
                Expiration = policy.Expiration;
            }

            return this;
        }
    }
}