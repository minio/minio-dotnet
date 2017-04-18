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

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Minio.DataModel.Policy
{

    internal class Statement

    {
        [JsonProperty("Action")]
        [JsonConverter(typeof(SingleOrArrayConverter<string>))]
        public IList<string> actions { get; set; }

        [JsonProperty("Condition")]
        public ConditionMap conditions { get; set; }

        [JsonProperty("Effect")]
        public string effect { get; set; }

        [JsonProperty("Principal")]
        [JsonConverter(typeof(PrincipalJsonConverter))]
        public Principal principal { get; set; }

        [JsonProperty("Resource")]
        [JsonConverter(typeof(ResourceJsonConverter))]
        public Resources resources { get; set; }

        [JsonProperty("Sid")]
        public string sid { get; set; }
        /**
        * Returns whether given statement is valid to process for given bucket name.
         */
        public bool isValid(string bucketName)
        {
            ISet<string> intersection;
            if (this.actions != null)
                intersection = new HashSet<string>(this.actions);
            else
                intersection = new HashSet<string>();

            intersection.IntersectWith(PolicyConstants.VALID_ACTIONS());

            if (intersection.Count == 0)
            {
                return false;
            }
            if (!this.effect.Equals("Allow"))
            {
                return false;
            }

            IList<string> aws = this.principal != null ? this.principal.aws() : null;

            if (aws == null || !aws.Contains("*"))
            {
                return false;
            }

            string bucketResource = PolicyConstants.AWS_RESOURCE_PREFIX + bucketName;

            if (this.resources == null)
            {
                return false;
            }

            if (this.resources.Contains(bucketResource))
            {
                return true;
            }

            if (this.resources.startsWith(bucketResource + "/").Count == 0)
            {
                return false;
            }

            return true;
        }

        /**
         * Removes object actions for given object resource.
         */
        public void removeObjectActions(string objectResource)
        {
            if (this.conditions != null)
            {
                return;
            }

            if (this.resources.Count > 1)
            {
                this.resources.Remove(objectResource);
            }
            else
            {
                this.actions.Except(PolicyConstants.READ_WRITE_OBJECT_ACTIONS());
            }
        }
        private void removeReadOnlyBucketActions(string prefix)
        {
            if (!utils.isSupersetOf(this.actions, PolicyConstants.READ_ONLY_BUCKET_ACTIONS))
            {
                return;
            }

            this.actions.Except(PolicyConstants.READ_ONLY_BUCKET_ACTIONS);

            if (this.conditions == null)
            {
                return;
            }

            if (prefix == null || prefix.Count() == 0)
            {
                return;
            }

            ConditionKeyMap stringEqualsValue;
            this.conditions.TryGetValue("StringEquals", out stringEqualsValue);
            if (stringEqualsValue == null)
            {
                return;
            }

            ISet<string> values;
            stringEqualsValue.TryGetValue("s3:prefix", out values);
            if (values != null)
            {
                values.Remove(prefix);
            }

            if (values == null || values.Count == 0)
            {
                stringEqualsValue.Remove("s3:prefix");
            }

            if (stringEqualsValue.Count == 0)
            {
                this.conditions.Remove("StringEquals");
            }

            if (this.conditions.Count == 0)
            {
                this.conditions = null;
            }
        }

        private void removeWriteOnlyBucketActions()
        {
            if (this.conditions == null)
            {
                this.actions.Except(PolicyConstants.WRITE_ONLY_BUCKET_ACTIONS);
            }
        }

        /**
        * Removes bucket actions for given prefix and bucketResource.
        */
        public void removeBucketActions(string prefix, string bucketResource,
                                    bool readOnlyInUse, bool writeOnlyInUse)
        {
            if (this.resources.Count > 1)
            {
                this.resources.Remove(bucketResource);
                return;
            }

            if (!readOnlyInUse)
            {
                removeReadOnlyBucketActions(prefix);
            }

            if (!writeOnlyInUse)
            {
                removeWriteOnlyBucketActions();
            }

            return;
        }

        /**
         * Returns bucket policy types for given prefix.
         */
        // [JsonIgnore]
        public bool[] getBucketPolicy(string prefix)
        {
            bool commonFound = false;
            bool readOnly = false;
            bool writeOnly = false;

            IList<string> aws = this.principal.aws();
            if (!(this.effect.Equals("Allow") && aws != null && aws.Contains("*")))
            {
                return new bool[] { commonFound, readOnly, writeOnly };
            }

            if (utils.isSupersetOf(this.actions, PolicyConstants.COMMON_BUCKET_ACTIONS) && this.conditions == null)
            {
                commonFound = true;
            }

            if (utils.isSupersetOf(this.actions, PolicyConstants.WRITE_ONLY_BUCKET_ACTIONS) && this.conditions == null)
            {
                writeOnly = true;
            }

            if (utils.isSupersetOf(this.actions, PolicyConstants.READ_ONLY_BUCKET_ACTIONS))
            {
                if (prefix != null && prefix.Count() != 0 && this.conditions != null)
                {
                    ConditionKeyMap stringEqualsValue;
                    this.conditions.TryGetValue("StringEquals", out stringEqualsValue);
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
                        this.conditions.TryGetValue("StringNotEquals", out stringNotEqualsValue);
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
                else if ((prefix == null || prefix.Count() == 0) && this.conditions == null)
                {
                    readOnly = true;
                }
                else if (prefix != null && prefix.Count() != 0 && this.conditions == null)
                {
                    readOnly = true;
                }
            }

            return new bool[] { commonFound, readOnly, writeOnly };
        }

        /**
        * Returns object policy types.
        */
        // [JsonIgnore]
        public bool[] getObjectPolicy()
        {
            bool readOnly = false;
            bool writeOnly = false;

            IList<string> aws = null;
            if (this.principal != null)
            {
                aws = this.principal.aws();
            }

            if (this.effect.Equals("Allow")
                && aws != null && aws.Contains("*")
                && this.conditions == null)
            {
                if (utils.isSupersetOf(this.actions, PolicyConstants.READ_ONLY_OBJECT_ACTIONS))
                {
                    readOnly = true;
                }
                if (utils.isSupersetOf(this.actions, PolicyConstants.WRITE_ONLY_OBJECT_ACTIONS))
                {
                    writeOnly = true;
                }
            }

            return new bool[] { readOnly, writeOnly };
        }

    }
}
