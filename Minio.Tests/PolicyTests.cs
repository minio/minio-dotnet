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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Minio.DataModel.Policy;
using Minio.DataModel;
using Newtonsoft.Json;

namespace Minio.Tests
{
    [TestClass]
    public class PolicyTests
    {
        [TestMethod]
        public void TestIfStatementIsValid()
        {
            var testCases = new List<KeyValuePair<List<Object>, bool>>()
            {
             
             // Empty statement and bucket name
             new KeyValuePair<List<Object>, bool>(new List<Object>{null, null, null, null,null, null },false),
            
             // Empty statement 
             new KeyValuePair<List<Object>, bool>(new List<Object>{"mybucket", null, null, null,null, null },false),
              
             // Empty bucketname
             new KeyValuePair<List<Object>, bool>(new List<Object>{null, PolicyConstants.READ_ONLY_BUCKET_ACTIONS, "Allow",new Principal("*"),new Resources("arn:aws:s3:::mybucket"), null },false),
            
             // Statement with unknown actions
             new KeyValuePair<List<Object>, bool>(new List<Object>{"mybucket", new List<string>() { "s3:ListBucketTypes" }, "Allow", new Principal("*"),new Resources("arn:aws:s3:::mybucket"), null },false),
             // Statement with unknown effect
             new KeyValuePair<List<Object>, bool>(new List<Object>{"mybucket", PolicyConstants.READ_ONLY_BUCKET_ACTIONS, "Deny", new Principal("*"),new Resources("arn:aws:s3:::mybucket"), null },false),
            
             // Statement with nil Principal
             new KeyValuePair<List<Object>, bool>(new List<Object>{"mybucket", PolicyConstants.READ_ONLY_BUCKET_ACTIONS, "Allow", null,new Resources("arn:aws:s3:::mybucket"), null },false),
            
             // Statement with invalid Principal
             new KeyValuePair<List<Object>, bool>(new List<Object>{"mybucket", PolicyConstants.READ_ONLY_BUCKET_ACTIONS, "Allow", new Principal("arn:aws:iam::AccountNumberWithoutHyphens:root"),new Resources("arn:aws:s3:::mybucket"), null },false),
           
             // Statement with different bucketname in resource 
             new KeyValuePair<List<Object>, bool>(new List<Object>{"mybucket", PolicyConstants.READ_ONLY_BUCKET_ACTIONS, "Allow", new Principal("*"),new Resources("arn:aws:s3:::bucket"), null },false),
             // Statement with incorrect bucketname in resource and suffixed string
             new KeyValuePair<List<Object>, bool>(new List<Object>{"mybucket", PolicyConstants.READ_ONLY_BUCKET_ACTIONS, "Allow", new Principal("*"), new Resources("arn:aws:s3:::mybuckettest/testobject"),new ConditionMap() },false),
             // Statement with bucket name and object name
             new KeyValuePair<List<Object>, bool>(new List<Object>{"mybucket", PolicyConstants.READ_ONLY_BUCKET_ACTIONS, "Allow", new Principal("*"), new Resources("arn:aws:s3:::mybucket/myobject"),new ConditionMap() },true),
             // Statement with conditions
             new KeyValuePair<List<Object>, bool>(new List<Object>{"mybucket", PolicyConstants.READ_ONLY_BUCKET_ACTIONS, "Allow", new Principal("*"), new Resources("arn:aws:s3:::mybucket"),new ConditionMap() },true),

            };
            int index = 0;
            foreach (KeyValuePair<List<Object>, bool> testCase in testCases)
            {
                index += 1;
                List<Object> data = testCase.Key;
                string bucketName = (string)data[0];

                List<string> actions = (List<string>)data[1];
                string effect = (string)data[2];
                Principal principal = (Principal)data[3];
                Resources resources = (Resources)data[4];
                ConditionMap conditionMap = (ConditionMap)data[5];
                bool isExpected = testCase.Value;

                //Set statement attributes
                Statement statement = new Statement();

                statement.actions = actions;
                statement.effect = effect;
                statement.principal = principal;
                statement.conditions = conditionMap;
                statement.resources = resources;
                bool isActual = statement.isValid(bucketName);
                Assert.AreEqual(isActual, isExpected);
            }

        }

        // Test Bucket Policy resource match
        [TestMethod]
        public void TestBucketPolicyResourceMatch()
        {
            string awsPrefix = PolicyConstants.AWS_RESOURCE_PREFIX;

            var testCases = new List<KeyValuePair<List<Object>, bool>>()
            {
             // Policy with resource ending with bucket /* allows access to all objects within given bucket.
             new KeyValuePair<List<Object>,bool>(new List<Object>{ TestHelper.GenerateResourcesPrefix("minio-bucket",""),
                                                                   TestHelper.GenerateStatement(awsPrefix + "minio-bucket/*"),
                                                                  }, 
                                                                  true),
             // Policy with resource ending with bucket/oo* should deny access to object named output.txt in that bucket
             new KeyValuePair<List<Object>,bool>(new List<Object>{ TestHelper.GenerateResourcesPrefix("minio-bucket","output.txt"),
                                                                   TestHelper.GenerateStatement(awsPrefix + "minio-bucket/oo*"),
                                                                  },
                                                                  false),
            // Policy with resource ending with bucket/oo* should allow access to object named ootput.txt in that bucket

            new KeyValuePair<List<Object>,bool>(new List<Object>{ TestHelper.GenerateResourcesPrefix("minio-bucket","ootput.txt"),
                                                                   TestHelper.GenerateStatement(awsPrefix + "minio-bucket/oo*"),
                                                                  },
                                                                  true),
            // Policy with resource ending with bucket/oo* allows access to all subfolders starting with "oo" inside given bucket. 
            new KeyValuePair<List<Object>,bool>(new List<Object>{ TestHelper.GenerateResourcesPrefix("minio-bucket","oops/output.txt"),
                                                                   TestHelper.GenerateStatement(awsPrefix + "minio-bucket/oo*"),
                                                                  },
                                                                  true),
            // Policy with resource subfolder not matching object subfolder.
            new KeyValuePair<List<Object>,bool>(new List<Object>{ TestHelper.GenerateResourcesPrefix("minio-bucket","test/mybad/output.txt"),
                                                                   TestHelper.GenerateStatement(awsPrefix + "minio-bucket/test/mybed/*"),
                                                                  },
                                                                  false),
            // Test names space flatness
            new KeyValuePair<List<Object>,bool>(new List<Object>{ TestHelper.GenerateResourcesPrefix("minio-bucket","Asia/India/MountK2/trip/sunrise.jpg"),
                                                                   TestHelper.GenerateStatement(awsPrefix + "minio-bucket/*/India/*/trip/*"),
                                                                  },
                                                                  true),
            new KeyValuePair<List<Object>,bool>(new List<Object>{ TestHelper.GenerateResourcesPrefix("minio-bucket","Asia/India/MountK2/trip/sunrise.jpg"),
                                                                   TestHelper.GenerateStatement(awsPrefix + "minio-bucket/*/India/*/sunrise.jpg"),
                                                                  },
                                                                  true),
            };
            int index = 0;
            foreach (KeyValuePair<List<Object>, bool> testCase in testCases)
            {
                index += 1;
                List<Object> data = testCase.Key;
                string resourcePrefix = (string)data[0];

                Statement stmt = (Statement)data[1];
                
                bool isExpected = testCase.Value;

                Resources matched = stmt.resources.Match(resourcePrefix);
                bool isActualMatch = matched.SetEquals(stmt.resources);
                Assert.AreEqual(isExpected, isActualMatch);
            }

        }
        [TestMethod]
        public void TestGetPolicy()
        {
            var testCases = new List<KeyValuePair<List<Object>, PolicyType>>()
            {
              
             // BucketPolicy NONE - empty statements, bucketname and prefix
             new KeyValuePair<List<Object>,PolicyType>(new List<Object>
             { new Statement(),"","" }, PolicyType.NONE),
           
                // BucketPolicy NONE - non empty statements, empty bucketname and empty prefix
             new KeyValuePair<List<Object>,PolicyType>(new List<Object>
             { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                            resourcePrefix:"arn:aws:s3:::mybucket"),
               "","" },PolicyType.NONE),
               // BucketPolicy NONE - empty statements, nonempty bucketname and empty prefix
             new KeyValuePair<List<Object>,PolicyType>(new List<Object>
             { new Statement(),"mybucket","" }, PolicyType.NONE),

               // BucketPolicy NONE - empty statements, empty bucketname and nonempty prefix
             new KeyValuePair<List<Object>,PolicyType>(new List<Object>
             { new Statement(),"","" }, PolicyType.NONE),
            
            // Not matching statements
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"testbucket","" },PolicyType.NONE),

             // Not matching statements with prefix
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","hello" },PolicyType.NONE),
            // Statements with only common bucket actions
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(PolicyConstants.COMMON_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","" },PolicyType.NONE),
            // Statements with only common bucket actions with prefix
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(PolicyConstants.COMMON_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","hello" },PolicyType.NONE),
            // Statements with only readonlybucketactions
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","" },PolicyType.NONE),
              // Statements with only readonlybucketactions with prefix
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","hello" },PolicyType.NONE),
            // Statements with only readonlybucketactions with conditions
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:true,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","" },PolicyType.NONE),
            // Statements with only readonlybucketactions with prefix and conditions
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:true,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","hello" },PolicyType.NONE),
            // Statements with only writeonlybucketactions 
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(PolicyConstants.WRITE_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","" },PolicyType.NONE),
             // Statements with only writeonlybucketactions with prefix
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(PolicyConstants.WRITE_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","hello" },PolicyType.NONE),
             // Statements with only writeonlybucketactions +readonlybucketactions
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(TestHelper.GetReadAndWriteBucketActions(),
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","" },PolicyType.NONE),
             // Statements with only writeonlybucketactions +readonlybucketactions and with prefix
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(TestHelper.GetReadAndWriteBucketActions(),
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","hello" },PolicyType.NONE),
            // Statements with only writeonlybucketactions +readonlybucketactions and with prefix and conditions
            new KeyValuePair<List<Object>,PolicyType>(new List<Object>
            { TestHelper.GenerateStatement(TestHelper.GetReadAndWriteBucketActions(),
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:true,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),"mybucket","hello" },PolicyType.NONE),
            };
            int index = 0;
            foreach (KeyValuePair<List<Object>, PolicyType> testCase in testCases)
            {
                index += 1;
                List<Object> data = testCase.Key;
                Statement statement = (Statement)data[0];

                string bucketName = (string)data[1];

                string prefix = (string)data[2];
                PolicyType expectedResult = (PolicyType)testCase.Value;
                BucketPolicy policy = new BucketPolicy(bucketName);
                policy.SetStatements(statement);
               
                Assert.IsTrue(expectedResult.Equals(policy.GetPolicy(prefix)));
            }
        }

        [TestMethod]
        public void TestGetBucketPolicy()
        {
            var testCases = new List<KeyValuePair<List<Object>, Tuple<bool, bool, bool>>>()
            {
                
                  // Statement with invalid effect
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                                 effect:"Deny",resourcePrefix:"arn:aws:s3:::mybucket"),
                      "mybucket","" }, Tuple.Create(false,false,false)),

                   // Statement with invalid effect with prefix
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                                 effect:"Deny",resourcePrefix:"arn:aws:s3:::mybucket"),
                      "mybucket","hello" }, Tuple.Create(false,false,false) ),

                  // Statement with invalid principal.aws
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                     effect:"Allow",aws:"arn:aws:iam::AccountNumberWithoutHyphens:root",resourcePrefix:"arn:aws:s3:::mybucket"),
                      "mybucket","" },  Tuple.Create(false,false,false)),
                  // Statement with invalid principal.aws with prefix
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                     effect:"Allow",aws:"arn:aws:iam::AccountNumberWithoutHyphens:root",resourcePrefix:"arn:aws:s3:::mybucket"),
                      "mybucket","hello" }, Tuple.Create(false,false,false)),
                  // Statement with common bucket actions
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.COMMON_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket"),
                      "mybucket","" },  Tuple.Create(true,false,false)),
                     // Statement with common bucket actions and prefix
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.COMMON_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket"),
                      "mybucket","hello" },  Tuple.Create(true,false,false)),
                  // Statement with common bucket actions and condition
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.COMMON_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true),
                      "mybucket","hello" },  Tuple.Create(false,false,false)),

                         // Statement with writeonly bucket actions
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.WRITE_ONLY_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:false),
                      "mybucket","" }, Tuple.Create(false,false,true)),
                     // Statement with writeonly bucket actions
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.WRITE_ONLY_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:false),
                      "mybucket","hello" }, Tuple.Create(false,false,true)),

                     // Statement with writeonly bucket actions with condition and no prefix
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.WRITE_ONLY_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true),
                      "mybucket","" }, Tuple.Create(false,false,false)),
                     // Statement with writeonly bucket actions with condition and prefix
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.WRITE_ONLY_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true),
                      "mybucket","hello" },  Tuple.Create(false,false,false)),

                   // Statement with Readonly bucket actions  
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:false),               
                      "mybucket","" }, Tuple.Create(false,true,false)),

                   // Statement with Readonly bucket actions  and prefix
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:false),
                      "mybucket","hello" }, Tuple.Create(false,true,false)),
                     // Statement with Readonly bucket actions with condition  
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true, withStringSet:null),
                      "mybucket","" }, Tuple.Create(false,false,false)),

                     // Statement with Readonly bucket actions with empty condition and prefix
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true, withStringSet:null),
                      "mybucket","hello" },  Tuple.Create(false,false,false)),

                  // Statement with Readonly bucket actions with matching condition and no prefix
                  new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                     effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true,withStringSet:"hello"),
                      "mybucket","" }, Tuple.Create(false,false,false)),
                   
               
                  // Statement with Readonly bucket actions with matching condition and  prefix
                new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                   effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true,withStringSet:"hello"),
                    "mybucket","hello" },  Tuple.Create(false,true,false)),
                 // Statement with Readonly bucket actions with different condition 
                new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                   effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true,withStringSet:"world"),
                    "mybucket","" }, Tuple.Create(false,false,false)),
                 // Statement with Readonly bucket actions with different condition 
                new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                   effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true,withStringSet:"world"),
                    "mybucket","hello" },  Tuple.Create(false,false,false)),
                 // Statement with Readonly bucket actions with StringNotEquals condition 
               new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                   effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true,withStringSet:"hello",condition:"StringNotEquals"),
                    "mybucket","" }, Tuple.Create(false,false,false)),
                   
                  // Statement with Readonly bucket actions with StringNotEquals condition 
                   new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                   effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true,withStringSet:"hello",condition:"StringNotEquals"),
                    "mybucket","" }, Tuple.Create(false,false,false)),
                     
                    // Statement with Readonly bucket actions with StringNotEquals condition 
                new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                   effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true,withStringSet:"hello",condition:"StringNotEquals"),
                    "mybucket","hello" }, Tuple.Create(false,false,false)),
                 new KeyValuePair<List<Object>,Tuple<bool,bool,bool>>(new List<Object>
                { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                   effect:"Allow",resourcePrefix:"arn:aws:s3:::mybucket", withConditions:true,withStringSet:"hello",condition:"StringNotEquals"),
                    "mybucket","world" }, Tuple.Create(false,true,false)),

            };
            int index = 0;
            foreach (KeyValuePair<List<Object>, Tuple<bool,bool,bool>> testCase in testCases)
            {
                index += 1;
                List<Object> data = testCase.Key;
                Statement statement = (Statement)data[0];
                string bucketName = (string)data[1];

                string prefix = (string)data[2];
                Tuple<bool,bool,bool> expectedResult = testCase.Value;
                bool[] actualResult = statement.getBucketPolicy(prefix);
                
                Assert.IsTrue(expectedResult.Item1.Equals(actualResult[0]));
                Assert.IsTrue(expectedResult.Item2.Equals(actualResult[1]));

                Assert.IsTrue(expectedResult.Item3.Equals(actualResult[2]));

            }
        }

        [TestMethod]
        public void TestGetObjectPolicy()
        {
            var testCases = new List<KeyValuePair<List<Object>, Tuple<bool, bool>>>()
            {
                
                  // Statement with invalid effect
                     new KeyValuePair<List<Object>,Tuple<bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_OBJECT_ACTIONS,
                                                 effect:"Deny",resourcePrefix:"arn:aws:s3:::mybucket/hello*"),
                    "mybucket","" }, Tuple.Create(false,false)),
                    // Statement with invalid Principal AWS
                  new KeyValuePair<List<Object>,Tuple<bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_OBJECT_ACTIONS,
                                                 effect:"Allow",aws:"arn:aws:iam::AccountNumberWithoutHyphens:root",resourcePrefix:"arn:aws:s3:::mybucket/hello*"),
                      "mybucket","" }, Tuple.Create(false,false)),
                   // Statement with condition
                   new KeyValuePair<List<Object>,Tuple<bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_OBJECT_ACTIONS,
                                                 effect:"Allow",
                                                 withConditions:true,condition: null,
                                                 resourcePrefix:"arn:aws:s3:::mybucket/hello*"),
                      "mybucket","" }, Tuple.Create(false,false)),
                    // Statement with readonlyobjectactions
                new KeyValuePair<List<Object>,Tuple<bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_OBJECT_ACTIONS,
                                                 effect:"Allow",
                                                 resourcePrefix:"arn:aws:s3:::mybucket/hello*"),
                      "mybucket","" }, Tuple.Create(true,false)),
                     // Statement with writeonlyobjectactions
                  new KeyValuePair<List<Object>,Tuple<bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.WRITE_ONLY_OBJECT_ACTIONS,
                                                 effect:"Allow",
                                                 resourcePrefix:"arn:aws:s3:::mybucket/hello*"),
                      "mybucket","" }, Tuple.Create(false,true)),
                    // Statement with writeonlyobjectactions
                  new KeyValuePair<List<Object>,Tuple<bool,bool>>(new List<Object>
                  { TestHelper.GenerateStatement(PolicyConstants.READ_WRITE_OBJECT_ACTIONS(),
                                                 effect:"Allow",
                                                 resourcePrefix:"arn:aws:s3:::mybucket/hello*"),
                      "mybucket","" }, Tuple.Create(true,true)),  
            };
            int index = 0;
            foreach (KeyValuePair<List<Object>, Tuple<bool, bool>> testCase in testCases)
            {
                index += 1;
                List<Object> data = testCase.Key;
                Statement statement = (Statement)data[0];
                string bucketName = (string)data[1];

                string prefix = (string)data[2];
                Tuple<bool, bool> expectedResult = testCase.Value;
                bool[] actualResult = statement.getObjectPolicy();

                Assert.IsTrue(expectedResult.Item1.Equals(actualResult[0]));
                Assert.IsTrue(expectedResult.Item2.Equals(actualResult[1]));
            }
        }

        [TestMethod]
        public void TestGetPolicies()
        {

            var testCases = new List<KeyValuePair<List<Object>, Dictionary<string, PolicyType>>>()
            {
              
             // BucketPolicy NONE - empty statements, bucketname and prefix
             new KeyValuePair<List<Object>,Dictionary<string,PolicyType>>(new List<Object>
             { new List<Statement>{new Statement() },""}, new Dictionary<string, PolicyType>{}),
           
                // BucketPolicy NONE - non empty statements, empty bucketname and empty prefix
             new KeyValuePair<List<Object>,Dictionary<string,PolicyType>>(new List<Object>
             { new List<Statement>{TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                            resourcePrefix:"arn:aws:s3:::mybucket") },
               "","" },new Dictionary<string, PolicyType>{}),
               // BucketPolicy NONE - empty statements, nonempty bucketname and empty prefix
             new KeyValuePair<List<Object>,Dictionary<string,PolicyType>>(new List<Object>
             { new List<Statement>{new Statement() },"mybucket","" }, new Dictionary<string, PolicyType>{}),

               // BucketPolicy NONE - empty statements, empty bucketname and nonempty prefix
             new KeyValuePair<List<Object>,Dictionary<string,PolicyType>>(new List<Object>
             { new List<Statement>{new Statement() },"","" }, new Dictionary<string, PolicyType>{}),

            // Statements with read bucket actions
            new KeyValuePair<List<Object>,Dictionary<string,PolicyType>>(new List<Object>
            { new List<Statement>{TestHelper.GenerateStatement(PolicyConstants.COMMON_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ) ,
                                  TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:true,
                                           withStringSet: "download",
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),
                                  TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_OBJECT_ACTIONS,
                                        effect:"Allow",
                                        aws:"*",
                                        withConditions:false,
                                        resourcePrefix: "arn:aws:s3:::mybucket/download*"
                                        )
            },"mybucket","" },new Dictionary<string, PolicyType>{{"mybucket/download*",PolicyType.READ_ONLY }}),
            // Statements with write only bucket actions
            new KeyValuePair<List<Object>,Dictionary<string,PolicyType>>(new List<Object>
            { new List<Statement>{TestHelper.GenerateStatement(PolicyConstants.COMMON_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ) ,
                                  TestHelper.GenerateStatement(PolicyConstants.WRITE_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           withStringSet: "download",
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),
                                  TestHelper.GenerateStatement(PolicyConstants.WRITE_ONLY_OBJECT_ACTIONS,
                                        effect:"Allow",
                                        aws:"*",
                                        withConditions:false,
                                        resourcePrefix: "arn:aws:s3:::mybucket/upload*"
                                        )
            },"mybucket","" },new Dictionary<string, PolicyType>{{"mybucket/upload*",PolicyType.WRITE_ONLY }}),
             // Statements with read-write bucket actions
            new KeyValuePair<List<Object>,Dictionary<string,PolicyType>>(new List<Object>
            { new List<Statement>{TestHelper.GenerateStatement(PolicyConstants.COMMON_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ) ,
                                  TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:true,
                                           withStringSet: "both",
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),
                                   TestHelper.GenerateStatement(PolicyConstants.WRITE_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),
                                  TestHelper.GenerateStatement(PolicyConstants.WRITE_ONLY_OBJECT_ACTIONS,
                                        effect:"Allow",
                                        aws:"*",
                                        withConditions:false,
                                        resourcePrefix: "arn:aws:s3:::mybucket/both*"
                                        ),
                                   TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_OBJECT_ACTIONS,
                                        effect:"Allow",
                                        aws:"*",
                                        withConditions:false,
                                        resourcePrefix: "arn:aws:s3:::mybucket/both*"
                                        )
            },"mybucket","" },new Dictionary<string, PolicyType>{{"mybucket/both*",PolicyType.READ_WRITE }})         
            };
            int index = 0;
            foreach (KeyValuePair<List<Object>, Dictionary<string,PolicyType>> testCase in testCases)
            {
                index += 1;
                List<Object> data = testCase.Key;
                List<Statement> statements = (List<Statement>)data[0];

                string bucketName = (string)data[1];
                Dictionary<string,PolicyType> expectedResult = testCase.Value;
                BucketPolicy policy = new BucketPolicy(bucketName);
                foreach (Statement statement in statements)
                    policy.statements.Add(statement);
                Dictionary<string, PolicyType> actualResult = policy.GetPolicies();
                Assert.IsTrue(expectedResult.PoliciesEqual(policy.GetPolicies()));
            }
        }


       
        [TestMethod]
        public void TestSetPolicy()
        {
            var testCases = new List<KeyValuePair<List<Object>, string>>()
            {
            
                 // BucketPolicy NONE - empty statements, bucketname and prefix
                 new KeyValuePair<List<Object>,string>(new List<Object>
                 { @"{""Statement"":[]}",
                   PolicyType.NONE,"","" }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),

                 // BucketPolicy NONE - non empty statements, empty bucketname and prefix
                 new KeyValuePair<List<Object>,string>(new List<Object>
                 { @"{""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}",
                   PolicyType.NONE,"","" },@"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}" ),
                // BucketPolicy NONE - empty statements, nonempty bucketname and prefix
                 new KeyValuePair<List<Object>,string>(new List<Object>
                 { @"{""Statement"":[]}",
                   PolicyType.NONE,"mybucket","" }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
            
                // Bucket policy NONE , empty statements , bucketname, nonempty prefix
                new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.NONE,"","prefix" }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
                // BucketPolicy READONLY - empty statements, bucket name and prefix.
                new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.READ_ONLY,"","" }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
                // Bucket policy READONLY , nonempty statements , bucketname and  prefix - no change to existing bucketpolicy
                new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}",
               PolicyType.READ_ONLY,"","" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}"),
           
                // BucketPolicy READONLY - empty statements, empty bucket name and prefix.
                new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.READ_ONLY,"mybucket","" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                    // BucketPolicy Writeonly - empty statements, bucket name and prefix.
               new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.WRITE_ONLY,"","" }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
                   // BucketPolicy Writeonly - empty statements, empty bucket name and non-empty prefix.
               new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.WRITE_ONLY,"","hello" }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
                
                 // Bucket policy WRITEONLY , nonempty statements , empty bucketname and  prefix - no change to existing bucketpolicy
                new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}",
               PolicyType.WRITE_ONLY,"","" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}"),           
                // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
                new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.WRITE_ONLY,"mybucket","" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                   // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
                new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.WRITE_ONLY,"mybucket","" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                 // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
                new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.WRITE_ONLY,"mybucket","hello" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/hello*""],""Sid"":""""}]}"),
              
               // BucketPolicy READWRITE - empty statements, empty bucket name and prefix.
                new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.READ_WRITE,"mybucket","" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:GetObject"",""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                    // BucketPolicy RERADWRITE - empty statements, bucket name and prefix.
            new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.READ_WRITE,"","" }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
                   // BucketPolicy READWRITE - empty statements, empty bucket name and non-empty prefix.
               new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
                    PolicyType.READ_WRITE,"","" }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),

                 // Bucket policy READWRITE , nonempty statements , empty bucketname and  prefix - no change to existing bucketpolicy
                new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}",
               PolicyType.READ_WRITE,"","" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}"),           
                // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
               new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.READ_WRITE,"mybucket","" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:GetObject"",""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                   // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
              new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.READ_WRITE,"mybucket","" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:GetObject"",""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/*""],""Sid"":""""}]}"),
                 // BucketPolicy WRITEONLY - empty statements, non-empty bucket name and prefix.
                  new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.READ_WRITE,"mybucket","hello" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucket""],""Condition"":{""StringEquals"":{""s3:prefix"":[""hello""]}},""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:ListBucketMultipartUploads""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""},{""Action"":[""s3:GetObject"",""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:ListMultipartUploadParts"",""s3:PutObject""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket/hello*""],""Sid"":""""}]}"),
          
            };
            int index = 0;
            foreach (KeyValuePair<List<Object>, string> testCase in testCases)
            {
                index += 1;
                List<Object> data = testCase.Key;
                PolicyType policyType = (PolicyType)data[1];
                string bucketName = (string)data[2];
                string prefix = (string)data[3];
                BucketPolicy currentpolicy = TestHelper.GenerateBucketPolicy((string)data[0], bucketName);
                currentpolicy.SetPolicy(policyType, prefix);
                string expectedResult = testCase.Value;
                string policyJSON = currentpolicy.GetJson();
                Assert.AreEqual(expectedResult, policyJSON);
            }
        }
        

    }
}
