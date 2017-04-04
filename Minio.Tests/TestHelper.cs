using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;
using Minio.DataModel.Policy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Minio.Tests
{
    class TestHelper
    {
        private static Random rnd = new Random();

        // Generate a random string
        public static String GetRandomName(int length = 5)
        {
            string characters = "0123456789abcdefghijklmnopqrstuvwxyz";
          
            StringBuilder result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(characters[rnd.Next(characters.Length)]);
            }
            return result.ToString();
        }

        internal static Statement GenerateStatement(string resource)
        {
            Statement stmt = new Statement();
            stmt.resources = new Resources(resource);
            return stmt;
        }

        internal static string GenerateResourcesPrefix(string bucketName, string objectName)
        {
            return PolicyConstants.AWS_RESOURCE_PREFIX + bucketName + "/" + objectName;
        }

        internal static Statement GenerateStatement(List<string> actions,string resourcePrefix, string effect = "Allow", string aws = "*",bool withConditions=false)
        {
            Statement stmt = new Statement();
            stmt.resources = new Resources(resourcePrefix);
            stmt.actions = actions;
            stmt.effect = effect;
            stmt.principal = new Principal(aws);
            if (withConditions)
            {
                stmt.conditions = new ConditionMap();
                ConditionKeyMap ckmap = new ConditionKeyMap();
                ckmap.Add("s3:prefix", new HashSet<string>() { "hello" });
                stmt.conditions.Add("StringEquals", ckmap);
            }
            
            return stmt;
        }

        internal static BucketPolicy GenerateBucketPolicy(string policyString,string bucketName)
        {
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(policyString);
            var stream = new MemoryStream(contentBytes);
            return BucketPolicy.ParseJson(stream, bucketName);

        }
        [TestMethod]
        public void GenPolicyForTest()
        {
            //List<string> actions, string resourcePrefix, string effect = "Allow", string aws = "*", bool withConditions = false)
            
            var testCases = new List<KeyValuePair<List<Object>, string>>()
            {
              
             // BucketPolicy NONE - empty statements, bucketname and prefix
             new KeyValuePair<List<Object>,string>(new List<Object>
             { new Statement(),
               PolicyType.NONE,"","" }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
           
                // BucketPolicy NONE - non empty statements, empty bucketname and prefix
             new KeyValuePair<List<Object>,string>(new List<Object>
             { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                            resourcePrefix:"arn:aws:s3:::mybucket"),
               PolicyType.NONE,"","" }, 
               @"{""Version"":""2012-10-17"",""Statement"":[{""Action"":[""s3:ListBucket""],""Effect"":""Allow"",""Principal"":{""AWS"":[""*""]},""Resource"":[""arn:aws:s3:::mybucket""],""Sid"":""""}]}"),

               // BucketPolicy NONE - empty statements, bucketname and prefix
             new KeyValuePair<List<Object>,string>(new List<Object>
             { new Statement(),
               PolicyType.NONE,"","" }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),

               // BucketPolicy NONE - empty statements, bucketname and prefix
             new KeyValuePair<List<Object>,string>(new List<Object>
             { new Statement(),
               PolicyType.NONE,"","" }, @"{""Version"":""2012-10-17"",""Statement"":[]}"),
            
            // Policy with resource ending with bucket /* allows access to all objects within given bucket.
            new KeyValuePair<List<Object>,string>(new List<Object>
            { TestHelper.GenerateStatement(PolicyConstants.READ_ONLY_BUCKET_ACTIONS,
                                           effect:"Allow",
                                           aws:"*",
                                           withConditions:false,
                                           resourcePrefix:"arn:aws:s3:::mybucket"
                                           ),
              PolicyType.NONE,"mybucket","" }, @""),
         
           
            };
            int index = 0;
            foreach (KeyValuePair<List<Object>, string> testCase in testCases)
            {
                index += 1;
                List<Object> data = testCase.Key;
                Statement statement = (Statement)data[0];

                PolicyType policyType = (PolicyType)data[1];
                string policyJSON = null;

                string bucketName = (string)data[2];
                string prefix = (string)data[3];
                string expectedResult = testCase.Value;
                BucketPolicy policy = new BucketPolicy(bucketName);
                policy.SetStatements(statement);
                policyJSON = policy.GetJson();
                Console.Out.WriteLine(@"before:""", policyJSON, @"""");

                policy.SetPolicy(policyType, prefix);
                policyJSON = policy.GetJson();
                Console.Out.WriteLine(@"after:""", policyJSON, @"""");
                Assert.AreEqual(expectedResult, policyJSON);
            }
        }
    }
}
