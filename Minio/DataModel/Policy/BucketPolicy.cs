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
    using System.IO;
    using System.Linq;
    using Minio.Helper;
    using Newtonsoft.Json;

    public class BucketPolicy
    {
        [JsonIgnore] private string bucketName;

        private List<Statement> statements;

        public BucketPolicy(string bucketName = null)
        {
            this.bucketName = bucketName;
        }

        [JsonProperty("Statement")]
        public List<Statement> Statements
        {
            get => this.statements ?? (this.statements = new List<Statement>());
            set => this.statements = value;
        }

        [JsonProperty("Version")]
        public string Version { get; set; } = "2012-10-17";

        /// <summary>
        ///     Reads JSON from given {@link Reader} and returns new {@link BucketPolicy} of given bucket name.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public static BucketPolicy ParseJson(MemoryStream reader, string bucketName)
        {
            var toparse = new StreamReader(reader).ReadToEnd();
            var bucketPolicy = JsonConvert.DeserializeObject<BucketPolicy>(toparse,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
            bucketPolicy.bucketName = bucketName;

            return bucketPolicy;
        }

        /// <summary>
        ///     Generates JSON of this BucketPolicy object.
        /// </summary>
        /// <returns></returns>
        public string GetJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
        }


        /// <summary>
        ///     Returns new bucket statements for given policy type.
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private List<Statement> NewBucketStatement(PolicyType policy, string prefix)
        {
            var stms = new List<Statement>();
            if (policy.Equals(PolicyType.None) || this.bucketName == null || this.bucketName.Length == 0)
            {
                return stms;
            }

            var resources = new Resources(PolicyConstants.AwsResourcePrefix + this.bucketName);
            var statement = new Statement
            {
                Actions = PolicyConstants.CommonBucketActions,
                Effect = "Allow",
                Principal = new Principal("*"),
                Resources = resources,
                Sid = ""
            };

            stms.Add(statement);

            if (policy.Equals(PolicyType.ReadOnly) || policy.Equals(PolicyType.ReadWrite))
            {
                statement = new Statement
                {
                    Actions = PolicyConstants.ReadOnlyBucketActions,
                    Effect = "Allow",
                    Principal = new Principal("*"),
                    Resources = resources,
                    Sid = ""
                };
                if (!string.IsNullOrEmpty(prefix))
                {
                    var map = new ConditionKeyMap();
                    map.Put("s3:prefix", prefix);
                    statement.Conditions = new ConditionMap("StringEquals", map);
                }

                stms.Add(statement);
            }

            if (policy.Equals(PolicyType.WriteOnly) || policy.Equals(PolicyType.ReadWrite))
            {
                statement = new Statement
                {
                    Actions = PolicyConstants.WriteOnlyBucketActions,
                    Effect = "Allow",
                    Principal = new Principal("*"),
                    Resources = resources,
                    Sid = ""
                };

                stms.Add(statement);
            }

            return stms;
        }


        /// <summary>
        ///     Returns new object statements for given policy type.
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private IEnumerable<Statement> NewObjectStatement(PolicyType policy, string prefix)
        {
            var stms = new List<Statement>();
            if (policy.Equals(PolicyType.None) || this.bucketName == null || this.bucketName.Length == 0)
            {
                return stms;
            }

            var resources = new Resources(PolicyConstants.AwsResourcePrefix + this.bucketName + "/" + prefix + "*");

            var statement = new Statement
            {
                Effect = "Allow",
                Principal = new Principal("*"),
                Resources = resources,
                Sid = ""
            };
            
            if (policy.Equals(PolicyType.ReadOnly))
            {
                statement.Actions = PolicyConstants.ReadOnlyObjectActions;
            }
            else if (policy.Equals(PolicyType.WriteOnly))
            {
                statement.Actions = PolicyConstants.WriteOnlyObjectActions;
            }
            else if (policy.Equals(PolicyType.ReadWrite))
            {
                statement.Actions = PolicyConstants.READ_WRITE_OBJECT_ACTIONS();
            }

            stms.Add(statement);
            return stms;
        }


        /// <summary>
        /// Returns new statements for given policy type.
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private IEnumerable<Statement> NewStatements(PolicyType policy, string prefix)
        {
            var stms = this.NewBucketStatement(policy, prefix);
            var objectStatements = this.NewObjectStatement(policy, prefix);

            stms.AddRange(objectStatements);

            return stms;
        }


        /// <summary>
        /// Returns whether statements are used by other than given prefix statements.
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private bool[] GetInUsePolicy(string prefix)
        {
            var resourcePrefix = PolicyConstants.AwsResourcePrefix + this.bucketName + "/";
            var objectResource = PolicyConstants.AwsResourcePrefix + this.bucketName + "/" + prefix + "*";

            var readOnlyInUse = false;
            var writeOnlyInUse = false;

            foreach (var statement in this.Statements)
            {
                if (!statement.Resources.Contains(objectResource)
                    && statement.Resources.StartsWith(resourcePrefix).Count != 0)
                {
                    if (Utils.IsSupersetOf(statement.Actions, PolicyConstants.ReadOnlyObjectActions))
                    {
                        readOnlyInUse = true;
                    }
                    if (Utils.IsSupersetOf(statement.Actions, PolicyConstants.WriteOnlyObjectActions))
                    {
                        writeOnlyInUse = true;
                    }
                }

                if (readOnlyInUse && writeOnlyInUse)
                {
                    break;
                }
            }

            bool[] rv = {readOnlyInUse, writeOnlyInUse};
            return rv;
        }


        /// <summary>
        /// Returns all statements of given prefix.
        /// </summary>
        /// <param name="prefix"></param>
        private void RemoveStatements(string prefix)
        {
            var bucketResource = PolicyConstants.AwsResourcePrefix + this.bucketName;
            var objectResource = PolicyConstants.AwsResourcePrefix + this.bucketName + "/" + prefix + "*";
            var inUse = this.GetInUsePolicy(prefix);
            var readOnlyInUse = inUse[0];
            var writeOnlyInUse = inUse[1];

            var outList = new List<Statement>();
            ISet<string> s3PrefixValues = new HashSet<string>();
            var readOnlyBucketStatements = new List<Statement>();

            foreach (var statement in this.Statements)
            {
                if (!statement.IsValid(this.bucketName))
                {
                    outList.Add(statement);
                    continue;
                }

                if (statement.Resources.Contains(bucketResource))
                {
                    if (statement.Conditions != null)
                    {
                        statement.RemoveBucketActions(prefix, bucketResource, false, false);
                    }
                    else
                    {
                        statement.RemoveBucketActions(prefix, bucketResource, readOnlyInUse, writeOnlyInUse);
                    }
                }
                else if (statement.Resources.Contains(objectResource))
                {
                    statement.RemoveObjectActions(objectResource);
                }

                if (statement.Actions.Count == 0)
                {
                    continue;
                }
                
                if (statement.Resources.Contains(bucketResource)
                    && Utils.IsSupersetOf(statement.Actions, PolicyConstants.ReadOnlyBucketActions)
                    && statement.Effect.Equals("Allow")
                    && statement.Principal.Aws().Contains("*"))
                {
                    if (statement.Conditions != null)
                    {
                        ConditionKeyMap stringEqualsValue;
                        statement.Conditions.TryGetValue("StringEquals", out stringEqualsValue);
                        if (stringEqualsValue != null)
                        {
                            ISet<string> values;
                            stringEqualsValue.TryGetValue("s3:prefix", out values);
                            if (values != null)
                            {
                                foreach (var v in values)
                                {
                                    s3PrefixValues.Add(bucketResource + "/" + v + "*");
                                }
                            }
                        }
                    }
                    else if (s3PrefixValues.Count != 0)
                    {
                        readOnlyBucketStatements.Add(statement);
                        continue;
                    }
                }

                outList.Add(statement);
            }

            var skipBucketStatement = true;
            var resourcePrefix = PolicyConstants.AwsResourcePrefix + this.bucketName + "/";
            foreach (var statement in outList)
            {
                ISet<string> intersection = new HashSet<string>(s3PrefixValues);
                intersection.IntersectWith(statement.Resources);

                if (!statement.Resources.StartsWith(resourcePrefix).Any() || intersection.Count != 0)
                {
                    continue;
                }
                
                skipBucketStatement = false;
                break;
            }

            foreach (var statement in readOnlyBucketStatements)
            {
                var aws = statement.Principal.Aws();
                if (skipBucketStatement
                    && statement.Resources.Contains(bucketResource)
                    && statement.Effect.Equals("Allow")
                    && aws != null && aws.Contains("*")
                    && statement.Conditions == null)
                {
                    continue;
                }

                outList.Add(statement);
            }

            if (outList.Count == 1)
            {
                var statement = outList[0];
                var aws = statement.Principal.Aws();
                if (statement.Resources.Contains(bucketResource)
                    && Utils.IsSupersetOf(statement.Actions, PolicyConstants.CommonBucketActions)
                    && statement.Effect.Equals("Allow")
                    && aws != null && aws.Contains("*")
                    && statement.Conditions == null)
                {
                    outList = new List<Statement>();
                }
            }

            this.Statements = outList;
        }

        /// <summary>
        /// Appends given statement into statement list to have unique statements.
        /// - If statement already exists in statement list, it ignores.
        /// - If statement exists with different conditions, they are merged.
        /// - Else the statement is appended to statement list.
        /// </summary>
        /// <param name="statement"></param>
        private void AppendStatement(Statement statement)
        {
            foreach (var s in this.Statements)
            {
                var aws = s.Principal.Aws();
                var conditions = s.Conditions;

                if (Utils.IsSupersetOf(s.Actions, statement.Actions)
                    && s.Effect.Equals(statement.Effect)
                    && aws != null && Utils.IsSupersetOf(aws, statement.Principal.Aws())
                    && conditions != null && conditions.Equals(statement.Conditions))
                {
                    s.Resources.UnionWith(statement.Resources);
                    return;
                }

                if (s.Resources.IsSupersetOf(statement.Resources)
                    && s.Effect.Equals(statement.Effect)
                    && aws != null && Utils.IsSupersetOf(aws, statement.Principal.Aws())
                    && conditions != null && conditions.Equals(statement.Conditions))
                {
                    s.Actions = s.Actions.Union(statement.Actions).ToList();
                    return;
                }

                if (s.Resources.IsSupersetOf(statement.Resources) && Utils.IsSupersetOf(s.Actions, statement.Actions) &&
                    s.Effect.Equals(statement.Effect) && aws != null &&
                    Utils.IsSupersetOf(aws, statement.Principal.Aws()))
                {
                    if (conditions != null && conditions.Equals(statement.Conditions))
                    {
                        return;
                    }

                    if (conditions != null && statement.Conditions != null)
                    {
                        conditions.PutAll(statement.Conditions);
                        return;
                    }
                }
            }
            if (statement.Actions != null && statement.Resources != null && statement.Actions.Count != 0 &&
                statement.Resources.Count != 0)
            {
                this.Statements.Add(statement);
            }
        }


        /// <summary>
        ///     Appends new statements for given policy type.
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="prefix"></param>
        private void AppendStatements(PolicyType policy, string prefix)
        {
            var appendStatements = this.NewStatements(policy, prefix);
            foreach (var statement in appendStatements)
            {
                this.AppendStatement(statement);
            }
        }


        /// <summary>
        ///     Returns policy type of this bucket policy.
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public PolicyType GetPolicy(string prefix)
        {
            var bucketResource = PolicyConstants.AwsResourcePrefix + this.bucketName;
            var objectResource = PolicyConstants.AwsResourcePrefix + this.bucketName + "/" + prefix + "*";

            var bucketCommonFound = false;
            var bucketReadOnly = false;
            var bucketWriteOnly = false;
            var matchedResource = "";
            var objReadOnly = false;
            var objWriteOnly = false;

            foreach (var s in this.Statements ?? new List<Statement>())
            {
                ISet<string> matchedObjResources = new HashSet<string>();

                if (s.Resources == null)
                {
                    continue;
                }

                if (s.Resources.Contains(objectResource))
                {
                    matchedObjResources.Add(objectResource);
                }
                else
                {
                    matchedObjResources = s.Resources.Match(objectResource);
                }

                if (matchedObjResources.Count != 0)
                {
                    var rv = s.GetObjectPolicy();
                    var readOnly = rv[0];
                    var writeOnly = rv[1];

                    foreach (var resource in matchedObjResources)
                    {
                        if (matchedResource.Length < resource.Length)
                        {
                            objReadOnly = readOnly;
                            objWriteOnly = writeOnly;
                            matchedResource = resource;
                        }
                        else if (matchedResource.Length == resource.Length)
                        {
                            objReadOnly = objReadOnly || readOnly;
                            objWriteOnly = objWriteOnly || writeOnly;
                            matchedResource = resource;
                        }
                    }
                }
                else if (s.Resources.Contains(bucketResource))
                {
                    var rv = s.GetBucketPolicy(prefix);
                    var commonFound = rv[0];
                    var readOnly = rv[1];
                    var writeOnly = rv[2];
                    bucketCommonFound = bucketCommonFound || commonFound;
                    bucketReadOnly = bucketReadOnly || readOnly;
                    bucketWriteOnly = bucketWriteOnly || writeOnly;
                }
            }

            if (!bucketCommonFound)
            {
                return PolicyType.None;
            }
            
            if (bucketReadOnly && bucketWriteOnly && objReadOnly && objWriteOnly)
            {
                return PolicyType.ReadWrite;
            }
            if (bucketReadOnly && objReadOnly)
            {
                return PolicyType.ReadOnly;
            }
            if (bucketWriteOnly && objWriteOnly)
            {
                return PolicyType.WriteOnly;
            }

            return PolicyType.None;
        }


        /// <summary>
        ///     Returns policy type of all prefixes.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, PolicyType> GetPolicies()
        {
            var policyRules = new Dictionary<string, PolicyType>();
            ISet<string> objResources = new HashSet<string>();

            var bucketResource = PolicyConstants.AwsResourcePrefix + this.bucketName;

            // Search all resources related to objects policy
            foreach (var s in this.Statements)
            {
                if (s.Resources != null)
                {
                    objResources.UnionWith(s.Resources.StartsWith(bucketResource + "/"));
                }
            }

            // Pretend that policy resource as an actual object and fetch its policy
            foreach (var r in objResources)
            {
                // Put trailing * if exists in asterisk
                var asterisk = "";
                var resource = r;
                if (r.EndsWith("*"))
                {
                    resource = r.Substring(0, r.Length - 1);
                    asterisk = "*";
                }

                // String objectPath = resource.Substring(bucketResource.Length + 1, resource.Length);
                var objectPath =
                    resource.Substring(bucketResource.Length + 1, resource.Length - bucketResource.Length - 1);

                var policy = this.GetPolicy(objectPath);
                policyRules.Add(this.bucketName + "/" + objectPath + asterisk, policy);
            }

            return policyRules;
        }


        /// <summary>
        ///     Sets policy type for given prefix.
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="prefix"></param>
        public void SetPolicy(PolicyType policy, string prefix)
        {
            if (this.Statements == null)
            {
                this.Statements = new List<Statement>();
            }

            this.RemoveStatements(prefix);
            this.AppendStatements(policy, prefix);
        }
    }
}