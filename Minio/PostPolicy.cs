/*
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
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Minio
{
    public class PostPolicy
    {
            private DateTime expiration;
            private List<Tuple<string, string, string>> policies =
                    new List<Tuple<string, string, string>>();
            private Dictionary<string, string> formData = new Dictionary<string, string>();
            public string key { get; private set; }
            public string bucket { get; private set; }

            public void SetExpires(DateTime expiration)
            {
                    this.expiration = expiration;
            }

            public void SetKey(string Key)
            {
                    if (string.IsNullOrEmpty(Key))
                    {
                            throw new ArgumentException("Object key cannot be null or empty");
                    }
                    this.policies.Add(new Tuple<string, string, string>("eq", "$key", Key));
                    this.formData.Add("key", Key);
                    this.key = Key;
            }

            public void SetKeyStartsWith(string KeyStartsWith)
            {
                    if (string.IsNullOrEmpty(KeyStartsWith))
                    {
                            throw new ArgumentException("Object key prefix cannot be null or empty");
                    }
                    this.policies.Add(new Tuple<string, string, string>("starts-with", "$key", KeyStartsWith));
                    this.formData.Add("key", KeyStartsWith);
            }

            public void SetBucket(string Bucket)
            {
                    if (string.IsNullOrEmpty(Bucket))
                    {
                            throw new ArgumentException("Bucket name cannot be null or empty");
                    }
                    this.policies.Add(new Tuple<string, string, string>("eq", "$bucket", Bucket));
                    this.formData.Add("bucket", Bucket);
                    this.bucket = Bucket;
            }

            public void SetContentType(string ContentType)
            {
                    if (string.IsNullOrEmpty(ContentType))
                    {
                            throw new ArgumentException("Content-Type argument cannot be null or empty");
                    }
                    this.policies.Add(new Tuple<string, string, string>("eq", "$Content-Type", ContentType));
                    this.formData.Add("Content-Type", ContentType);
            }

            public void SetAlgorithm(string algorithm)
            {
                if (string.IsNullOrEmpty(algorithm))
                {
                        throw new ArgumentException("Algorithm argument cannot be null or empty");
                }
                this.policies.Add(new Tuple<string, string, string>("eq", "$x-amz-algorithm", algorithm));
                this.formData.Add("x-amz-algorithm", algorithm);
            }

            public void SetCredential(string credential)
            {
                if (string.IsNullOrEmpty(credential))
                {
                        throw new ArgumentException("credential argument cannot be null or empty");
                }
                this.policies.Add(new Tuple<string, string, string>("eq", "$x-amz-credential", credential));
                this.formData.Add("x-amz-credential", credential);
            }

            public void SetDate(DateTime date)
            {
                string dateStr = date.ToString("yyyyMMddTHHmmssZ");
                this.policies.Add(new Tuple<string, string, string>("eq", "$x-amz-date", dateStr));
                this.formData.Add("x-amz-date", dateStr);
            }

            public void SetPolicy(string policy)
            {
                this.formData.Add("policy", policy);
            }

            public void SetSignature(string signature)
            {
                this.formData.Add("x-amz-signature", signature);
            }

            private byte[] marshalJSON()
            {
                    List<string> policies = new List<string>();
                    StringBuilder sb = new StringBuilder();

                    foreach (var policy in this.policies)
                    {
                            policies.Add("[\"" + policy.Item1 + "\",\"" + policy.Item2 + "\",\"" + policy.Item3+"\"]");
                    }
                    // expiration and conditions will never be empty because of checks at PresignedPostPolicy()
                    sb.Append("{");
                    sb.Append("\"expiration\":\"").Append(this.expiration.ToString("yyyy-MM-ddTHH:mm:ss.000Z")).Append("\"").Append(",");
                    sb.Append("\"conditions\":[").Append(String.Join(",", policies)).Append("]");
                    sb.Append("}");
                    return System.Text.Encoding.UTF8.GetBytes(sb.ToString() as string);
            }

            public string Base64()
            {
                    byte[] policyStrBytes = this.marshalJSON();
                    return Convert.ToBase64String(policyStrBytes);
            }

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

            public bool IsExpirationSet()
            {
                    if (!string.IsNullOrEmpty(this.expiration.ToString()))
                    {
                            return true;
                    }
                    return false;
            }

            public Dictionary<string, string> getFormData() {
                return this.formData;
            }
    }
}
