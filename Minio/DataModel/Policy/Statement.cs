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

namespace Minio.DataModel.Policy
{
    using System.Collections.Generic;
    using System.Linq;
    using Minio.Helper;
    using Newtonsoft.Json;

    public class Statement
    {
        [JsonProperty("Action")]
        [JsonConverter(typeof(SingleOrArrayConverter<string>))]
        public IList<string> Actions { get; set; }

        [JsonProperty("Condition")]
        public ConditionMap Conditions { get; set; }

        [JsonProperty("Effect")]
        public string Effect { get; set; }

        [JsonProperty("Principal")]
        [JsonConverter(typeof(PrincipalJsonConverter))]
        public Principal Principal { get; set; }

        [JsonProperty("Resource")]
        [JsonConverter(typeof(ResourceJsonConverter))]
        public Resources Resources { get; set; }

        [JsonProperty("Sid")]
        public string Sid { get; set; }

        /**
        * Returns whether given statement is valid to process for given bucket name.
         */
        public bool IsValid(string bucketName)
        {
            ISet<string> intersection;
            intersection = this.Actions != null ? new HashSet<string>(this.Actions) : new HashSet<string>();
            intersection.IntersectWith(PolicyConstants.VALID_ACTIONS());

            if (intersection.Count == 0)
            {
                return false;
            }
            if (!this.Effect.Equals("Allow"))
            {
                return false;
            }

            var aws = this.Principal?.Aws();

            if (aws == null || !aws.Contains("*"))
            {
                return false;
            }

            var bucketResource = PolicyConstants.AwsResourcePrefix + bucketName;

            if (this.Resources == null)
            {
                return false;
            }

            if (this.Resources.Contains(bucketResource))
            {
                return true;
            }

            if (this.Resources.StartsWith(bucketResource + "/").Count == 0)
            {
                return false;
            }

            return true;
        }

        /**
         * Removes object actions for given object resource.
         */
        public void RemoveObjectActions(string objectResource)
        {
            if (this.Conditions != null)
            {
                return;
            }

            if (this.Resources.Count > 1)
            {
                this.Resources.Remove(objectResource);
            }
            else
            {
                this.Actions = this.Actions.Except(PolicyConstants.READ_WRITE_OBJECT_ACTIONS())?.ToList();
            }
        }

        private void RemoveReadOnlyBucketActions(string prefix)
        {
            if (!Utils.IsSupersetOf(this.Actions, PolicyConstants.ReadOnlyBucketActions))
            {
                return;
            }

            this.Actions = this.Actions.Except(PolicyConstants.ReadOnlyBucketActions)?.ToList();
            if (this.Conditions == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(prefix))
            {
                return;
            }

            ConditionKeyMap stringEqualsValue;
            this.Conditions.TryGetValue("StringEquals", out stringEqualsValue);
            if (stringEqualsValue == null)
            {
                return;
            }

            ISet<string> values;
            stringEqualsValue.TryGetValue("s3:prefix", out values);
            values?.Remove(prefix);

            if (values == null || values.Count == 0)
            {
                stringEqualsValue.Remove("s3:prefix");
            }

            if (stringEqualsValue.Count == 0)
            {
                this.Conditions.Remove("StringEquals");
            }

            if (this.Conditions.Count == 0)
            {
                this.Conditions = null;
            }
        }

        private void RemoveWriteOnlyBucketActions()
        {
            if (this.Conditions == null)
            {
                this.Actions = this.Actions.Except(PolicyConstants.WriteOnlyBucketActions)?.ToList();
            }
        }

        /**
        * Removes bucket actions for given prefix and bucketResource.
        */
        public void RemoveBucketActions(string prefix, string bucketResource,
            bool readOnlyInUse, bool writeOnlyInUse)
        {
            if (this.Resources.Count > 1)
            {
                this.Resources.Remove(bucketResource);
                return;
            }

            if (!readOnlyInUse)
            {
                this.RemoveReadOnlyBucketActions(prefix);
            }

            if (!writeOnlyInUse)
            {
                this.RemoveWriteOnlyBucketActions();
            }
        }

        /**
         * Returns bucket policy types for given prefix.
         */
        public bool[] GetBucketPolicy(string prefix)
        {
            var commonFound = false;
            var readOnly = false;
            var writeOnly = false;

            var aws = this.Principal.Aws();
            if (!(this.Effect.Equals("Allow") && aws != null && aws.Contains("*")))
            {
                return new[] {commonFound, readOnly, writeOnly};
            }

            if (Utils.IsSupersetOf(this.Actions, PolicyConstants.CommonBucketActions) && this.Conditions == null)
            {
                commonFound = true;
            }

            if (Utils.IsSupersetOf(this.Actions, PolicyConstants.WriteOnlyBucketActions) && this.Conditions == null)
            {
                writeOnly = true;
            }

            if (Utils.IsSupersetOf(this.Actions, PolicyConstants.ReadOnlyBucketActions))
            {
                if (!string.IsNullOrEmpty(prefix) && this.Conditions != null)
                {
                    ConditionKeyMap stringEqualsValue;
                    this.Conditions.TryGetValue("StringEquals", out stringEqualsValue);
                    if (stringEqualsValue != null)
                    {
                        ISet<string> s3PrefixValues;
                        stringEqualsValue.TryGetValue("s3:prefix", out s3PrefixValues);
                        if (s3PrefixValues != null && s3PrefixValues.Contains(prefix))
                        {
                            readOnly = true;
                        }
                    }
                    else
                    {
                        ConditionKeyMap stringNotEqualsValue;
                        this.Conditions.TryGetValue("StringNotEquals", out stringNotEqualsValue);
                        if (stringNotEqualsValue != null)
                        {
                            ISet<string> s3PrefixValues;
                            stringNotEqualsValue.TryGetValue("s3:prefix", out s3PrefixValues);
                            if (s3PrefixValues != null && !s3PrefixValues.Contains(prefix))
                            {
                                readOnly = true;
                            }
                        }
                    }
                }
                else if (string.IsNullOrEmpty(prefix) && this.Conditions == null)
                {
                    readOnly = true;
                }
                else if (!string.IsNullOrEmpty(prefix) && this.Conditions == null)
                {
                    readOnly = true;
                }
            }

            return new[] {commonFound, readOnly, writeOnly};
        }

        /**
        * Returns object policy types.
        */
        // [JsonIgnore]
        public bool[] GetObjectPolicy()
        {
            var readOnly = false;
            var writeOnly = false;

            IList<string> aws = null;
            if (this.Principal != null)
            {
                aws = this.Principal.Aws();
            }

            if (this.Effect.Equals("Allow")
                && aws != null && aws.Contains("*")
                && this.Conditions == null)
            {
                if (Utils.IsSupersetOf(this.Actions, PolicyConstants.ReadOnlyObjectActions))
                {
                    readOnly = true;
                }
                if (Utils.IsSupersetOf(this.Actions, PolicyConstants.WriteOnlyObjectActions))
                {
                    writeOnly = true;
                }
            }

            return new[] {readOnly, writeOnly};
        }
    }
}