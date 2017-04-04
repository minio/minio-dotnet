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
                Console.Out.WriteLine(matched);
                Console.Out.WriteLine(stmt.resources);
                Assert.AreEqual(isExpected, isActualMatch);
            }

        }

        [TestMethod]
        public void TestSetPolicy()
        {
            var testCases = new List<KeyValuePair<List<Object>, string>>()
            {
                /* 
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
            */
                // BucketPolicy READONLY - empty statements, non-empty bucket name and prefix.
                new KeyValuePair<List<Object>,string>(new List<Object>
                { @"{""Statement"":[]}",
               PolicyType.READ_ONLY,"mybucket","" }, @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{""AWS"":["" * ""]},""Resource"":[""arn: aws: s3:::mybucket""],""Sid"":""""}]}"),


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
        /*

        [TestMethod]
        public void TestNewStatement()
        {
            var testCases = new List<KeyValuePair<List<Object>,string>>()
            {
             
             // Empty statement and bucket name
             new KeyValuePair<List<Object>,string>(new List<Object>{"", PolicyType.NONE, "" }, "{}"),
                     
             new KeyValuePair<List<Object>,string>(new List<Object>{"mybucket",PolicyType.READ_ONLY,"hello" },"expected"),
       
            };
            int index = 0;
            foreach (KeyValuePair<List<Object>, string> testCase in testCases)
            {
                index += 1;
                List<Object> data = testCase.Key;
                string bucketName = (string)data[0];

               PolicyType policyType = (PolicyType) data[1];
                string prefix = (string)data[2];
               
                string expected = testCase.Value;

                //Set statement attributes

                
                //policy.
              
                string stmtJSON = JsonConvert.SerializeObject(statement, Formatting.None,
                                     new JsonSerializerSettings
                                     {
                                         NullValueHandling = NullValueHandling.Ignore
                                     });

                Assert.AreEqual(expected, stmtJSON);
            }

        }
        */
    }
}
