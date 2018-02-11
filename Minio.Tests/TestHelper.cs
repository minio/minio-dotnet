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

using Minio.DataModel;
using Minio.DataModel.Policy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        // Generate an empty statement
        internal static Statement GenerateStatement(string resource)
        {
            Statement stmt = new Statement();
            stmt.resources = new Resources(resource);
            return stmt;
        }

        // Generate a resource prefix
        internal static string GenerateResourcesPrefix(string bucketName, string objectName)
        {
            return PolicyConstants.AWS_RESOURCE_PREFIX + bucketName + "/" + objectName;
        }

        // Generate a new statement
        internal static Statement GenerateStatement(List<string> actions,string resourcePrefix, string effect = "Allow", string aws = "*",bool withConditions=false,string withStringSet="hello",string condition="StringEquals")
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
                if (withStringSet != null)
                    ckmap.Add("s3:prefix", new HashSet<string>() {withStringSet });
                if (condition != null && ckmap != null)
                    stmt.conditions.Add(condition, ckmap);
            }
            
            return stmt;
        }
        // Get List with Read and Write bucket actions 
        internal static List<string> GetReadAndWriteBucketActions()
        {
            List<string> res = new List<string>();
            res.AddRange(PolicyConstants.READ_ONLY_BUCKET_ACTIONS);
            res.AddRange(PolicyConstants.WRITE_ONLY_BUCKET_ACTIONS);
            return res;
        }
        // Hydrate a bucket policy from JSON string 
        internal static BucketPolicy GenerateBucketPolicy(string policyString,string bucketName)
        {
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(policyString);
            var stream = new MemoryStream(contentBytes);
            return BucketPolicy.ParseJson(stream, bucketName);

        }
     
    }
}
