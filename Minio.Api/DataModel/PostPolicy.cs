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

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Minio.DataModel
{
    public class PostPolicy
    {
            private DateTime expiration;
            private List<Tuple<string, string, string>> policies =
                    new List<Tuple<string, string, string>>();
            private Dictionary<string, string> formData = new Dictionary<string, string>();
            public string Key { get; private set; }
            public string Bucket { get; private set; }

            /// <summary>
            /// Set expiration policy.
            /// </summary>
            /// <param name="expiration">Expiration time for the policy</param>
            public void SetExpires(DateTime expiration)
            {
                    this.expiration = expiration;
            }

            /// <summary>
            /// Set key policy.
            /// </summary>
            /// <param name="Key">Object name for the policy</param>
            public void SetKey(string key)
            {
                    if (string.IsNullOrEmpty(key))
                    {
                            throw new ArgumentException("Object key cannot be null or empty");
                    }
                    this.policies.Add(new Tuple<string, string, string>("eq", "$key", key));
                    this.formData.Add("key", key);
                    this.Key = key;
            }

            /// <summary>
            /// Set key prefix policy.
            /// </summary>
            /// <param name="KeyStartsWith">Object name prefix for the policy</param>
            public void SetKeyStartsWith(string keyStartsWith)
            {
                    if (string.IsNullOrEmpty(keyStartsWith))
                    {
                            throw new ArgumentException("Object key prefix cannot be null or empty");
                    }
                    this.policies.Add(new Tuple<string, string, string>("starts-with", "$key", keyStartsWith));
                    this.formData.Add("key", keyStartsWith);
            }

            /// <summary>
            /// Set bucket policy.
            /// </summary>
            /// <param name="Bucket">Bucket name for the policy</param>
            public void SetBucket(string bucket)
            {
                    if (string.IsNullOrEmpty(bucket))
                    {
                            throw new ArgumentException("Bucket name cannot be null or empty");
                    }
                    this.policies.Add(new Tuple<string, string, string>("eq", "$bucket", bucket));
                    this.formData.Add("bucket", bucket);
                    this.Bucket = bucket;
            }

            /// <summary>
            /// Set content type policy.
            /// </summary>
            /// <param name="ContentType">ContentType for the policy</param>
            public void SetcontentType(string contentType)
            {
                    if (string.IsNullOrEmpty(contentType))
                    {
                            throw new ArgumentException("Content-Type argument cannot be null or empty");
                    }
                    this.policies.Add(new Tuple<string, string, string>("eq", "$Content-Type", contentType));
                    this.formData.Add("Content-Type", contentType);
            }

            /// <summary>
            /// Set signature algorithm policy.
            /// </summary>
            /// <param name="algorithm">Set signature algorithm used for the policy</param>
            public void SetAlgorithm(string algorithm)
            {
                if (string.IsNullOrEmpty(algorithm))
                {
                        throw new ArgumentException("Algorithm argument cannot be null or empty");
                }
                this.policies.Add(new Tuple<string, string, string>("eq", "$x-amz-algorithm", algorithm));
                this.formData.Add("x-amz-algorithm", algorithm);
            }

            /// <summary>
            /// Set credential policy.
            /// </summary>
            /// <param name="credential">Set credential string for the policy</param>
            public void SetCredential(string credential)
            {
                if (string.IsNullOrEmpty(credential))
                {
                        throw new ArgumentException("credential argument cannot be null or empty");
                }
                this.policies.Add(new Tuple<string, string, string>("eq", "$x-amz-credential", credential));
                this.formData.Add("x-amz-credential", credential);
            }

            /// <summary>
            /// Set date policy.
            /// </summary>
            /// <param name="date">Set date for the policy</param>
            public void SetDate(DateTime date)
            {
                string dateStr = date.ToString("yyyyMMddTHHmmssZ");
                this.policies.Add(new Tuple<string, string, string>("eq", "$x-amz-date", dateStr));
                this.formData.Add("x-amz-date", dateStr);
            }

            /// <summary>
            /// Set base64 encoded policy to form dictionary.
            /// </summary>
            /// <param name="policy">Base64 encoded policy</param>
            public void SetPolicy(string policyBase64)
            {
                this.formData.Add("policy", policyBase64);
            }

            /// <summary>
            /// Set computed signature for the policy to form dictionary.
            /// </summary>
            /// <param name="signature">Computed signature</param>
            public void SetSignature(string signature)
            {
                this.formData.Add("x-amz-signature", signature);
            }

            /// <summary>
            /// Serialize policy into JSON string.
            /// </summary>
            /// <returns>Serialized JSON policy</returns>
            private byte[] marshalJSON()
            {
                    List<string> policyList = new List<string>();
                    StringBuilder sb = new StringBuilder();

                    foreach (var policy in this.policies)
                    {
                            policyList.Add("[\"" + policy.Item1 + "\",\"" + policy.Item2 + "\",\"" + policy.Item3+"\"]");
                    }
                    // expiration and conditions will never be empty because of checks at PresignedPostPolicy()
                    sb.Append("{");
                    sb.Append("\"expiration\":\"").Append(this.expiration.ToString("yyyy-MM-ddTHH:mm:ss.000Z")).Append("\"").Append(",");
                    sb.Append("\"conditions\":[").Append(String.Join(",", policyList)).Append("]");
                    sb.Append("}");
                    return System.Text.Encoding.UTF8.GetBytes(sb.ToString() as string);
            }

            /// <summary>
            /// Compute base64 encoded form of JSON policy.
            /// </summary>
            /// <returns>Base64 encoded string of JSON policy</returns>
            public string Base64()
            {
                    byte[] policyStrBytes = this.marshalJSON();
                    return Convert.ToBase64String(policyStrBytes);
            }

            /// <summary>
            /// Verify if bucket is set in policy.
            /// </summary>
            /// <returns>true if bucket is set</returns>
            public bool IsBucketSet()
            {
                    string value = "";
                    if (this.formData.TryGetValue("bucket", out value))
                    {
                            if (!string.IsNullOrEmpty(value))
                            {
                                    return true;
                            }
                    }
                    return false;
            }

            /// <summary>
            /// Verify if key is set in policy.
            /// </summary>
            /// <returns>true if key is set</returns>
            public bool IsKeySet()
            {
                    string value = "";
                    if (this.formData.TryGetValue("key", out value))
                    {
                            if (!string.IsNullOrEmpty(value))
                            {
                                    return true;
                            }
                    }
                    return false;
            }

            /// <summary>
            /// Verify if expiration is set in policy.
            /// </summary>
            /// <returns>true if expiration is set</returns>
            public bool IsExpirationSet()
            {
                    if (!string.IsNullOrEmpty(this.expiration.ToString()))
                    {
                            return true;
                    }
                    return false;
            }

            /// <summary>
            /// Get the populated dictionary of policy data.
            /// </summary>
            /// <returns>Dictionary of policy data</returns>
            public Dictionary<string, string> GetFormData() {
                return this.formData;
            }
    }
}
