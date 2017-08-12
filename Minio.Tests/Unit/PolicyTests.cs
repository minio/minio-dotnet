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

namespace Minio.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using DataModel.Policy;
    using Newtonsoft.Json.Linq;
    using Xunit;

    public class PolicyTests
    {
        // Test Bucket Policy resource match
        [Fact]
        public void TestBucketPolicyResourceMatch()
        {
            var awsPrefix = PolicyConstants.AwsResourcePrefix;

            var testCases = new List<KeyValuePair<List<object>, bool>>
            {
                // Policy with resource ending with bucket /* allows access to all objects within given bucket.
                new KeyValuePair<List<object>, bool>(new List<object>
                    {
                        TestHelper.GenerateResourcesPrefix("minio-bucket", ""),
                        TestHelper.GenerateStatement(awsPrefix + "minio-bucket/*")
                    },
                    true),
                // Policy with resource ending with bucket/oo* should deny access to object named output.txt in that bucket
                new KeyValuePair<List<object>, bool>(new List<object>
                    {
                        TestHelper.GenerateResourcesPrefix("minio-bucket", "output.txt"),
                        TestHelper.GenerateStatement(awsPrefix + "minio-bucket/oo*")
                    },
                    false),
                // Policy with resource ending with bucket/oo* should allow access to object named ootput.txt in that bucket

                new KeyValuePair<List<object>, bool>(new List<object>
                    {
                        TestHelper.GenerateResourcesPrefix("minio-bucket", "ootput.txt"),
                        TestHelper.GenerateStatement(awsPrefix + "minio-bucket/oo*")
                    },
                    true),
                // Policy with resource ending with bucket/oo* allows access to all subfolders starting with "oo" inside given bucket. 
                new KeyValuePair<List<object>, bool>(new List<object>
                    {
                        TestHelper.GenerateResourcesPrefix("minio-bucket", "oops/output.txt"),
                        TestHelper.GenerateStatement(awsPrefix + "minio-bucket/oo*")
                    },
                    true),
                // Policy with resource subfolder not matching object subfolder.
                new KeyValuePair<List<object>, bool>(new List<object>
                    {
                        TestHelper.GenerateResourcesPrefix("minio-bucket", "test/mybad/output.txt"),
                        TestHelper.GenerateStatement(awsPrefix + "minio-bucket/test/mybed/*")
                    },
                    false),
                // Test names space flatness
                new KeyValuePair<List<object>, bool>(new List<object>
                    {
                        TestHelper.GenerateResourcesPrefix("minio-bucket", "Asia/India/MountK2/trip/sunrise.jpg"),
                        TestHelper.GenerateStatement(awsPrefix + "minio-bucket/*/India/*/trip/*")
                    },
                    true),
                new KeyValuePair<List<object>, bool>(new List<object>
                    {
                        TestHelper.GenerateResourcesPrefix("minio-bucket", "Asia/India/MountK2/trip/sunrise.jpg"),
                        TestHelper.GenerateStatement(awsPrefix + "minio-bucket/*/India/*/sunrise.jpg")
                    },
                    true)
            };
            var index = 0;
            foreach (var testCase in testCases)
            {
                index += 1;
                var data = testCase.Key;
                var resourcePrefix = (string)data[0];

                var stmt = (Statement)data[1];

                var isExpected = testCase.Value;

                var matched = stmt.Resources.Match(resourcePrefix);
                var isActualMatch = matched.SetEquals(stmt.Resources);
                Assert.Equal(isExpected, isActualMatch);
            }
        }

        [Fact]
        public void TestGetBucketPolicy()
        {
            var testCases = new List<KeyValuePair<List<object>, Tuple<bool, bool, bool>>>
            {
                // Statement with invalid effect
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Deny", resourcePrefix: "arn:aws:s3:::mybucket"),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false, false)),

                // Statement with invalid effect with prefix
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Deny", resourcePrefix: "arn:aws:s3:::mybucket"),
                    "mybucket",
                    "hello"
                }, Tuple.Create(false, false, false)),

                // Statement with invalid principal.aws
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", aws: "arn:aws:iam::AccountNumberWithoutHyphens:root",
                        resourcePrefix: "arn:aws:s3:::mybucket"),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false, false)),
                // Statement with invalid principal.aws with prefix
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", aws: "arn:aws:iam::AccountNumberWithoutHyphens:root",
                        resourcePrefix: "arn:aws:s3:::mybucket"),
                    "mybucket",
                    "hello"
                }, Tuple.Create(false, false, false)),
                // Statement with common bucket actions
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.CommonBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket"),
                    "mybucket",
                    ""
                }, Tuple.Create(true, false, false)),
                // Statement with common bucket actions and prefix
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.CommonBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket"),
                    "mybucket",
                    "hello"
                }, Tuple.Create(true, false, false)),
                // Statement with common bucket actions and condition
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.CommonBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true),
                    "mybucket",
                    "hello"
                }, Tuple.Create(false, false, false)),

                // Statement with writeonly bucket actions
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.WriteOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: false),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false, true)),
                // Statement with writeonly bucket actions
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.WriteOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: false),
                    "mybucket",
                    "hello"
                }, Tuple.Create(false, false, true)),

                // Statement with writeonly bucket actions with condition and no prefix
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.WriteOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false, false)),
                // Statement with writeonly bucket actions with condition and prefix
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.WriteOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true),
                    "mybucket",
                    "hello"
                }, Tuple.Create(false, false, false)),

                // Statement with Readonly bucket actions  
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: false),
                    "mybucket",
                    ""
                }, Tuple.Create(false, true, false)),

                // Statement with Readonly bucket actions  and prefix
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: false),
                    "mybucket",
                    "hello"
                }, Tuple.Create(false, true, false)),
                // Statement with Readonly bucket actions with condition  
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true,
                        withStringSet: null),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false, false)),

                // Statement with Readonly bucket actions with empty condition and prefix
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true,
                        withStringSet: null),
                    "mybucket",
                    "hello"
                }, Tuple.Create(false, false, false)),

                // Statement with Readonly bucket actions with matching condition and no prefix
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true,
                        withStringSet: "hello"),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false, false)),


                // Statement with Readonly bucket actions with matching condition and  prefix
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true,
                        withStringSet: "hello"),
                    "mybucket",
                    "hello"
                }, Tuple.Create(false, true, false)),
                // Statement with Readonly bucket actions with different condition 
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true,
                        withStringSet: "world"),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false, false)),
                // Statement with Readonly bucket actions with different condition 
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true,
                        withStringSet: "world"),
                    "mybucket",
                    "hello"
                }, Tuple.Create(false, false, false)),
                // Statement with Readonly bucket actions with StringNotEquals condition 
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true,
                        withStringSet: "hello", condition: "StringNotEquals"),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false, false)),

                // Statement with Readonly bucket actions with StringNotEquals condition 
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true,
                        withStringSet: "hello", condition: "StringNotEquals"),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false, false)),

                // Statement with Readonly bucket actions with StringNotEquals condition 
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true,
                        withStringSet: "hello", condition: "StringNotEquals"),
                    "mybucket",
                    "hello"
                }, Tuple.Create(false, false, false)),
                new KeyValuePair<List<object>, Tuple<bool, bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow", resourcePrefix: "arn:aws:s3:::mybucket", withConditions: true,
                        withStringSet: "hello", condition: "StringNotEquals"),
                    "mybucket",
                    "world"
                }, Tuple.Create(false, true, false))
            };
            var index = 0;
            foreach (var testCase in testCases)
            {
                index += 1;
                var data = testCase.Key;
                var statement = (Statement)data[0];
                var bucketName = (string)data[1];

                var prefix = (string)data[2];
                var expectedResult = testCase.Value;
                var actualResult = statement.GetBucketPolicy(prefix);

                Assert.True(expectedResult.Item1.Equals(actualResult[0]));
                Assert.True(expectedResult.Item2.Equals(actualResult[1]));

                Assert.True(expectedResult.Item3.Equals(actualResult[2]));
            }
        }

        [Fact]
        public void TestGetObjectPolicy()
        {
            var testCases = new List<KeyValuePair<List<object>, Tuple<bool, bool>>>
            {
                // Statement with invalid effect
                new KeyValuePair<List<object>, Tuple<bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyObjectActions,
                        effect: "Deny", resourcePrefix: "arn:aws:s3:::mybucket/hello*"),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false)),
                // Statement with invalid Principal AWS
                new KeyValuePair<List<object>, Tuple<bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyObjectActions,
                        effect: "Allow", aws: "arn:aws:iam::AccountNumberWithoutHyphens:root",
                        resourcePrefix: "arn:aws:s3:::mybucket/hello*"),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false)),
                // Statement with condition
                new KeyValuePair<List<object>, Tuple<bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyObjectActions,
                        effect: "Allow",
                        withConditions: true, condition: null,
                        resourcePrefix: "arn:aws:s3:::mybucket/hello*"),
                    "mybucket",
                    ""
                }, Tuple.Create(false, false)),
                // Statement with readonlyobjectactions
                new KeyValuePair<List<object>, Tuple<bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyObjectActions,
                        effect: "Allow",
                        resourcePrefix: "arn:aws:s3:::mybucket/hello*"),
                    "mybucket",
                    ""
                }, Tuple.Create(true, false)),
                // Statement with writeonlyobjectactions
                new KeyValuePair<List<object>, Tuple<bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.WriteOnlyObjectActions,
                        effect: "Allow",
                        resourcePrefix: "arn:aws:s3:::mybucket/hello*"),
                    "mybucket",
                    ""
                }, Tuple.Create(false, true)),
                // Statement with writeonlyobjectactions
                new KeyValuePair<List<object>, Tuple<bool, bool>>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.READ_WRITE_OBJECT_ACTIONS(),
                        effect: "Allow",
                        resourcePrefix: "arn:aws:s3:::mybucket/hello*"),
                    "mybucket",
                    ""
                }, Tuple.Create(true, true))
            };
            var index = 0;
            foreach (var testCase in testCases)
            {
                index += 1;
                var data = testCase.Key;
                var statement = (Statement)data[0];
                var bucketName = (string)data[1];

                var prefix = (string)data[2];
                var expectedResult = testCase.Value;
                var actualResult = statement.GetObjectPolicy();
                Assert.True(expectedResult.Item1.Equals(actualResult[0]));
                Assert.True(expectedResult.Item2.Equals(actualResult[1]));
            }
        }

        [Fact]
        public void TestGetPolicies()
        {
            var testCases = new List<KeyValuePair<List<object>, Dictionary<string, PolicyType>>>
            {
                // BucketPolicy NONE - empty statements, bucketname and prefix
                new KeyValuePair<List<object>, Dictionary<string, PolicyType>>(new List<object>
                    {new List<Statement> {new Statement()}, ""}, new Dictionary<string, PolicyType>()),

                // BucketPolicy NONE - non empty statements, empty bucketname and empty prefix
                new KeyValuePair<List<object>, Dictionary<string, PolicyType>>(new List<object>
                {
                    new List<Statement>
                    {
                        TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                            "arn:aws:s3:::mybucket")
                    },
                    "",
                    ""
                }, new Dictionary<string, PolicyType>()),
                // BucketPolicy NONE - empty statements, nonempty bucketname and empty prefix
                new KeyValuePair<List<object>, Dictionary<string, PolicyType>>(new List<object>
                    {new List<Statement> {new Statement()}, "mybucket", ""}, new Dictionary<string, PolicyType>()),

                // BucketPolicy NONE - empty statements, empty bucketname and nonempty prefix
                new KeyValuePair<List<object>, Dictionary<string, PolicyType>>(new List<object>
                    {new List<Statement> {new Statement()}, "", ""}, new Dictionary<string, PolicyType>()),

                // Statements with read bucket actions
                new KeyValuePair<List<object>, Dictionary<string, PolicyType>>(new List<object>
                {
                    new List<Statement>
                    {
                        TestHelper.GenerateStatement(PolicyConstants.CommonBucketActions,
                            effect: "Allow",
                            aws: "*",
                            withConditions: false,
                            resourcePrefix: "arn:aws:s3:::mybucket"
                        ),
                        TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                            effect: "Allow",
                            aws: "*",
                            withConditions: true,
                            withStringSet: "download",
                            resourcePrefix: "arn:aws:s3:::mybucket"
                        ),
                        TestHelper.GenerateStatement(PolicyConstants.ReadOnlyObjectActions,
                            effect: "Allow",
                            aws: "*",
                            withConditions: false,
                            resourcePrefix: "arn:aws:s3:::mybucket/download*"
                        )
                    },
                    "mybucket",
                    ""
                }, new Dictionary<string, PolicyType> {{"mybucket/download*", PolicyType.ReadOnly}}),
                // Statements with write only bucket actions
                new KeyValuePair<List<object>, Dictionary<string, PolicyType>>(new List<object>
                {
                    new List<Statement>
                    {
                        TestHelper.GenerateStatement(PolicyConstants.CommonBucketActions,
                            effect: "Allow",
                            aws: "*",
                            withConditions: false,
                            resourcePrefix: "arn:aws:s3:::mybucket"
                        ),
                        TestHelper.GenerateStatement(PolicyConstants.WriteOnlyBucketActions,
                            effect: "Allow",
                            aws: "*",
                            withConditions: false,
                            withStringSet: "download",
                            resourcePrefix: "arn:aws:s3:::mybucket"
                        ),
                        TestHelper.GenerateStatement(PolicyConstants.WriteOnlyObjectActions,
                            effect: "Allow",
                            aws: "*",
                            withConditions: false,
                            resourcePrefix: "arn:aws:s3:::mybucket/upload*"
                        )
                    },
                    "mybucket",
                    ""
                }, new Dictionary<string, PolicyType> {{"mybucket/upload*", PolicyType.WriteOnly}}),
                // Statements with read-write bucket actions
                new KeyValuePair<List<object>, Dictionary<string, PolicyType>>(new List<object>
                {
                    new List<Statement>
                    {
                        TestHelper.GenerateStatement(PolicyConstants.CommonBucketActions,
                            effect: "Allow",
                            aws: "*",
                            withConditions: false,
                            resourcePrefix: "arn:aws:s3:::mybucket"
                        ),
                        TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                            effect: "Allow",
                            aws: "*",
                            withConditions: true,
                            withStringSet: "both",
                            resourcePrefix: "arn:aws:s3:::mybucket"
                        ),
                        TestHelper.GenerateStatement(PolicyConstants.WriteOnlyBucketActions,
                            effect: "Allow",
                            aws: "*",
                            resourcePrefix: "arn:aws:s3:::mybucket"
                        ),
                        TestHelper.GenerateStatement(PolicyConstants.WriteOnlyObjectActions,
                            effect: "Allow",
                            aws: "*",
                            withConditions: false,
                            resourcePrefix: "arn:aws:s3:::mybucket/both*"
                        ),
                        TestHelper.GenerateStatement(PolicyConstants.ReadOnlyObjectActions,
                            effect: "Allow",
                            aws: "*",
                            withConditions: false,
                            resourcePrefix: "arn:aws:s3:::mybucket/both*"
                        )
                    },
                    "mybucket",
                    ""
                }, new Dictionary<string, PolicyType> {{"mybucket/both*", PolicyType.ReadWrite}})
            };
            var index = 0;
            foreach (var testCase in testCases)
            {
                index += 1;
                var data = testCase.Key;
                var statements = (List<Statement>)data[0];

                var bucketName = (string)data[1];
                var expectedResult = testCase.Value;
                var policy = new BucketPolicy(bucketName);
                foreach (var statement in statements)
                {
                    policy.Statements.Add(statement);
                }

                Assert.True(expectedResult.PoliciesEqual(policy.GetPolicies()));
            }
        }

        [Fact]
        public void TestGetPolicy()
        {
            var testCases = new List<KeyValuePair<List<object>, PolicyType>>
            {
                // BucketPolicy NONE - empty statements, bucketname and prefix
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                    {new Statement(), "", ""}, PolicyType.None),

                // BucketPolicy NONE - non empty statements, empty bucketname and empty prefix
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        "arn:aws:s3:::mybucket"),
                    "",
                    ""
                }, PolicyType.None),
                // BucketPolicy NONE - empty statements, nonempty bucketname and empty prefix
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                    {new Statement(), "mybucket", ""}, PolicyType.None),

                // BucketPolicy NONE - empty statements, empty bucketname and nonempty prefix
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                    {new Statement(), "", ""}, PolicyType.None),

                // Not matching statements
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow",
                        aws: "*",
                        withConditions: false,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "testbucket",
                    ""
                }, PolicyType.None),

                // Not matching statements with prefix
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow",
                        aws: "*",
                        withConditions: false,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    "hello"
                }, PolicyType.None),
                // Statements with only common bucket actions
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.CommonBucketActions,
                        effect: "Allow",
                        aws: "*",
                        withConditions: false,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    ""
                }, PolicyType.None),
                // Statements with only common bucket actions with prefix
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.CommonBucketActions,
                        effect: "Allow",
                        aws: "*",
                        withConditions: false,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    "hello"
                }, PolicyType.None),
                // Statements with only readonlybucketactions
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow",
                        aws: "*",
                        withConditions: false,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    ""
                }, PolicyType.None),
                // Statements with only readonlybucketactions with prefix
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow",
                        aws: "*",
                        withConditions: false,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    "hello"
                }, PolicyType.None),
                // Statements with only readonlybucketactions with conditions
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow",
                        aws: "*",
                        withConditions: true,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    ""
                }, PolicyType.None),
                // Statements with only readonlybucketactions with prefix and conditions
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.ReadOnlyBucketActions,
                        effect: "Allow",
                        aws: "*",
                        withConditions: true,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    "hello"
                }, PolicyType.None),
                // Statements with only writeonlybucketactions 
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.WriteOnlyBucketActions,
                        effect: "Allow",
                        aws: "*",
                        withConditions: false,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    ""
                }, PolicyType.None),
                // Statements with only writeonlybucketactions with prefix
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(PolicyConstants.WriteOnlyBucketActions,
                        effect: "Allow",
                        aws: "*",
                        withConditions: false,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    "hello"
                }, PolicyType.None),
                // Statements with only writeonlybucketactions +readonlybucketactions
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(TestHelper.GetReadAndWriteBucketActions(),
                        effect: "Allow",
                        aws: "*",
                        withConditions: false,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    ""
                }, PolicyType.None),
                // Statements with only writeonlybucketactions +readonlybucketactions and with prefix
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(TestHelper.GetReadAndWriteBucketActions(),
                        effect: "Allow",
                        aws: "*",
                        withConditions: false,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    "hello"
                }, PolicyType.None),
                // Statements with only writeonlybucketactions +readonlybucketactions and with prefix and conditions
                new KeyValuePair<List<object>, PolicyType>(new List<object>
                {
                    TestHelper.GenerateStatement(TestHelper.GetReadAndWriteBucketActions(),
                        effect: "Allow",
                        aws: "*",
                        withConditions: true,
                        resourcePrefix: "arn:aws:s3:::mybucket"
                    ),
                    "mybucket",
                    "hello"
                }, PolicyType.None)
            };
            var index = 0;
            foreach (var testCase in testCases)
            {
                index += 1;
                var data = testCase.Key;
                var statement = (Statement)data[0];

                var bucketName = (string)data[1];

                var prefix = (string)data[2];
                var expectedResult = testCase.Value;
                var policy = new BucketPolicy(bucketName);
                policy.Statements.Add(statement);
                Assert.True(expectedResult.Equals(policy.GetPolicy(prefix)));
            }
        }

        [Fact]
        public void TestIfStatementIsValid()
        {
            var testCases = new List<KeyValuePair<List<object>, bool>>
            {
                // Empty statement and bucket name
                new KeyValuePair<List<object>, bool>(new List<object> {null, null, null, null, null, null}, false),

                // Empty statement 
                new KeyValuePair<List<object>, bool>(new List<object> {"mybucket", null, null, null, null, null},
                    false),

                // Empty bucketname
                new KeyValuePair<List<object>, bool>(
                    new List<object>
                    {
                        null,
                        PolicyConstants.ReadOnlyBucketActions,
                        "Allow",
                        new Principal("*"),
                        new Resources("arn:aws:s3:::mybucket"),
                        null
                    }, false),

                // Statement with unknown actions
                new KeyValuePair<List<object>, bool>(
                    new List<object>
                    {
                        "mybucket",
                        new List<string> {"s3:ListBucketTypes"},
                        "Allow",
                        new Principal("*"),
                        new Resources("arn:aws:s3:::mybucket"),
                        null
                    }, false),
                // Statement with unknown effect
                new KeyValuePair<List<object>, bool>(
                    new List<object>
                    {
                        "mybucket",
                        PolicyConstants.ReadOnlyBucketActions,
                        "Deny",
                        new Principal("*"),
                        new Resources("arn:aws:s3:::mybucket"),
                        null
                    }, false),

                // Statement with nil Principal
                new KeyValuePair<List<object>, bool>(
                    new List<object>
                    {
                        "mybucket",
                        PolicyConstants.ReadOnlyBucketActions,
                        "Allow",
                        null,
                        new Resources("arn:aws:s3:::mybucket"),
                        null
                    }, false),

                // Statement with invalid Principal
                new KeyValuePair<List<object>, bool>(
                    new List<object>
                    {
                        "mybucket",
                        PolicyConstants.ReadOnlyBucketActions,
                        "Allow",
                        new Principal("arn:aws:iam::AccountNumberWithoutHyphens:root"),
                        new Resources("arn:aws:s3:::mybucket"),
                        null
                    }, false),

                // Statement with different bucketname in resource 
                new KeyValuePair<List<object>, bool>(
                    new List<object>
                    {
                        "mybucket",
                        PolicyConstants.ReadOnlyBucketActions,
                        "Allow",
                        new Principal("*"),
                        new Resources("arn:aws:s3:::bucket"),
                        null
                    }, false),
                // Statement with incorrect bucketname in resource and suffixed string
                new KeyValuePair<List<object>, bool>(
                    new List<object>
                    {
                        "mybucket",
                        PolicyConstants.ReadOnlyBucketActions,
                        "Allow",
                        new Principal("*"),
                        new Resources("arn:aws:s3:::mybuckettest/testobject"),
                        new ConditionMap()
                    }, false),
                // Statement with bucket name and object name
                new KeyValuePair<List<object>, bool>(
                    new List<object>
                    {
                        "mybucket",
                        PolicyConstants.ReadOnlyBucketActions,
                        "Allow",
                        new Principal("*"),
                        new Resources("arn:aws:s3:::mybucket/myobject"),
                        new ConditionMap()
                    }, true),
                // Statement with conditions
                new KeyValuePair<List<object>, bool>(
                    new List<object>
                    {
                        "mybucket",
                        PolicyConstants.ReadOnlyBucketActions,
                        "Allow",
                        new Principal("*"),
                        new Resources("arn:aws:s3:::mybucket"),
                        new ConditionMap()
                    }, true)
            };
            var index = 0;
            foreach (var testCase in testCases)
            {
                index += 1;
                var data = testCase.Key;
                var bucketName = (string)data[0];

                var actions = (List<string>)data[1];
                var effect = (string)data[2];
                var principal = (Principal)data[3];
                var resources = (Resources)data[4];
                var conditionMap = (ConditionMap)data[5];
                var isExpected = testCase.Value;

                //Set statement attributes
                var statement = new Statement();

                statement.Actions = actions;
                statement.Effect = effect;
                statement.Principal = principal;
                statement.Conditions = conditionMap;
                statement.Resources = resources;
                var isActual = statement.IsValid(bucketName);
                Assert.Equal(isActual, isExpected);
            }
        }


        [Fact]
        public void TestSetPolicy()
        {
            var testCases = new List<KeyValuePair<List<object>, string>>
            {
                // BucketPolicy NONE - empty statements, bucketname and prefix
                new KeyValuePair<List<object>, string>(new List<object>
                {
                    @"{""Statement"":[]}",
                    PolicyType.None,
                    "",
                    ""
                }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),

                // BucketPolicy NONE - non empty statements, empty bucketname and prefix
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}",
                        PolicyType.None,
                        "",
                        ""
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}"),
                // BucketPolicy NONE - empty statements, nonempty bucketname and prefix
                new KeyValuePair<List<object>, string>(new List<object>
                {
                    @"{""Statement"":[]}",
                    PolicyType.None,
                    "mybucket",
                    ""
                }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),

                // Bucket policy NONE , empty statements , bucketname, nonempty prefix
                new KeyValuePair<List<object>, string>(new List<object>
                {
                    @"{""Statement"":[]}",
                    PolicyType.None,
                    "",
                    "prefix"
                }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
                // BucketPolicy READONLY - empty statements, bucket name and prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                {
                    @"{""Statement"":[]}",
                    PolicyType.ReadOnly,
                    "",
                    ""
                }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
                // Bucket policy READONLY , nonempty statements , bucketname and  prefix - no change to existing bucketpolicy
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}",
                        PolicyType.ReadOnly,
                        "",
                        ""
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}"),

                // BucketPolicy READONLY - empty statements, empty bucket name and prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[]}",
                        PolicyType.ReadOnly,
                        "mybucket",
                        ""
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                // BucketPolicy Writeonly - empty statements, bucket name and prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                {
                    @"{""Statement"":[]}",
                    PolicyType.WriteOnly,
                    "",
                    ""
                }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
                // BucketPolicy Writeonly - empty statements, empty bucket name and non-empty prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                {
                    @"{""Statement"":[]}",
                    PolicyType.WriteOnly,
                    "",
                    "hello"
                }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),

                // Bucket policy WRITEONLY , nonempty statements , empty bucketname and  prefix - no change to existing bucketpolicy
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}",
                        PolicyType.WriteOnly,
                        "",
                        ""
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}"),
                // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[]}",
                        PolicyType.WriteOnly,
                        "mybucket",
                        ""
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[]}",
                        PolicyType.WriteOnly,
                        "mybucket",
                        ""
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[]}",
                        PolicyType.WriteOnly,
                        "mybucket",
                        "hello"
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/hello*""],""Sid"":""""}]}"),

                // BucketPolicy READWRITE - empty statements, empty bucket name and prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[]}",
                        PolicyType.ReadWrite,
                        "mybucket",
                        ""
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:GetObject"",""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                // BucketPolicy RERADWRITE - empty statements, bucket name and prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                {
                    @"{""Statement"":[]}",
                    PolicyType.ReadWrite,
                    "",
                    ""
                }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
                // BucketPolicy READWRITE - empty statements, empty bucket name and non-empty prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                {
                    @"{""Statement"":[]}",
                    PolicyType.ReadWrite,
                    "",
                    ""
                }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),

                // Bucket policy READWRITE , nonempty statements , empty bucketname and  prefix - no change to existing bucketpolicy
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}",
                        PolicyType.ReadWrite,
                        "",
                        ""
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}"),
                // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[]}",
                        PolicyType.ReadWrite,
                        "mybucket",
                        ""
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:GetObject"",""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[]}",
                        PolicyType.ReadWrite,
                        "mybucket",
                        ""
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:GetObject"",""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
                new KeyValuePair<List<object>, string>(new List<object>
                    {
                        @"{""Statement"":[]}",
                        PolicyType.ReadWrite,
                        "mybucket",
                        "hello"
                    },
                    @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucket""],""Condition"":{""StringEquals"":{""s3:prefix"":[""hello""]}},""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:GetObject"",""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/hello*""],""Sid"":""""}]}")
            };
            var index = 0;
            foreach (var testCase in testCases)
            {
                index += 1;
                var data = testCase.Key;
                var policyType = (PolicyType)data[1];
                var bucketName = (string)data[2];
                var prefix = (string)data[3];
                var currentpolicy = TestHelper.GenerateBucketPolicy((string)data[0], bucketName);
                currentpolicy.SetPolicy(policyType, prefix);
                var expectedResult = testCase.Value;
                var policyJSON = currentpolicy.GetJson();
                Assert.True(JObject.DeepEquals(JObject.Parse(expectedResult), JObject.Parse(policyJSON)));
            }
        }
    }
}