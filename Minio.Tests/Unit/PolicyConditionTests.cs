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
    using System.IO;
    using System.Text;
    using DataModel.Policy;
    using Newtonsoft.Json;
    using Xunit;

    public class PolicyConditionTests
    {
        [Fact]
        public void TestConditionKeyMapAdd()
        {
            var testCases = new List<KeyValuePair<Tuple<string, HashSet<string>>, string>>
            {
                // Add new k-v pair
                new KeyValuePair<Tuple<string, HashSet<string>>, string>(
                    new Tuple<string, HashSet<string>>("s3:prefix", new HashSet<string> {"hello"}),
                    @"{""s3:prefix"":[""hello""]}"),
                // Add existing k-v pair
                new KeyValuePair<Tuple<string, HashSet<string>>, string>(
                    new Tuple<string, HashSet<string>>("s3:prefix", new HashSet<string> {"hello"}),
                    @"{""s3:prefix"":[""hello""]}"),
                // Add existing key and not value
                new KeyValuePair<Tuple<string, HashSet<string>>, string>(
                    new Tuple<string, HashSet<string>>("s3:prefix", new HashSet<string> {"world"}),
                    @"{""s3:prefix"":[""hello"", ""world""]}")
            };
            var cmap = new ConditionKeyMap();
            var index = 0;
            foreach (var pair in testCases)
            {
                try
                {
                    index += 1;
                    var testcase = pair.Key;
                    var prefix = testcase.Item1;
                    var stringSet = testcase.Item2;
                    var expectedConditionKMap = pair.Value;
                    cmap.Add(prefix, stringSet);
                    var cmpstring = JsonConvert.SerializeObject(cmap, Formatting.None,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });

                    Assert.Equal(cmpstring, expectedConditionKMap);
                }
                catch (ArgumentException)
                {
                    Assert.NotEqual(index, 1);
                }
            }
        }

        [Fact]
        // Tests if condition key map merges existing values 
        public void TestConditionKeyMapPut()
        {
            var cmap1 = new ConditionKeyMap();
            cmap1.Add("s3:prefix", new HashSet<string> {"hello"});

            var cmap2 = new ConditionKeyMap();
            cmap2.Add("s3:prefix", new HashSet<string> {"world"});

            var cmap3 = new ConditionKeyMap();
            cmap3.Add("s3:myprefix", new HashSet<string> {"world"});

            var cmap4 = new ConditionKeyMap();
            cmap4.Add("s3:prefix", new HashSet<string> {"hello"});
            var testCases = new List<KeyValuePair<Tuple<ConditionKeyMap, ConditionKeyMap>, string>>
            {
                // Both args are empty
                new KeyValuePair<Tuple<ConditionKeyMap, ConditionKeyMap>, string>(
                    Tuple.Create(new ConditionKeyMap(), new ConditionKeyMap()), @"{}"),
                // First arg empty
                new KeyValuePair<Tuple<ConditionKeyMap, ConditionKeyMap>, string>(
                    Tuple.Create(new ConditionKeyMap(), cmap1), @"{""s3:prefix"":[""hello""]}"),
                //Second arg empty
                new KeyValuePair<Tuple<ConditionKeyMap, ConditionKeyMap>, string>(
                    Tuple.Create(cmap1, new ConditionKeyMap()), @"{""s3:prefix"":[""hello""]}"),
                //Both args have same value
                new KeyValuePair<Tuple<ConditionKeyMap, ConditionKeyMap>, string>(Tuple.Create(cmap1, cmap4),
                    @"{""s3:prefix"":[""hello""]}"),
                //Value of second arg will be merged
                new KeyValuePair<Tuple<ConditionKeyMap, ConditionKeyMap>, string>(Tuple.Create(cmap1, cmap2),
                    @"{""s3:prefix"":[""hello"",""world""]}"),
                //second arg will be added 
                new KeyValuePair<Tuple<ConditionKeyMap, ConditionKeyMap>, string>(Tuple.Create(cmap1, cmap3),
                    @"{""s3:prefix"":[""hello"",""world""],""s3:myprefix"":[""world""]}")
            };


            var index = 0;
            foreach (var pair in testCases)
            {
                try
                {
                    index += 1;
                    var testcase = pair.Key;
                    var first = testcase.Item1;
                    var second = testcase.Item2;
                    var expectedConditionKMapJSON = pair.Value;
                    foreach (var kvpair in second)
                    {
                        first.Put(kvpair.Key, kvpair.Value);
                    }
                    var cmpstring = JsonConvert.SerializeObject(first, Formatting.None,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });

                    Assert.Equal(cmpstring, expectedConditionKMapJSON);
                }
                catch (ArgumentException)
                {
                    throw;
                }
            }
        }

        [Fact]
        public void TestConditionKeyMapRemove()
        {
            var testCases = new List<KeyValuePair<Tuple<string, HashSet<string>>, string>>
            {
                // Add new k-v pair
                new KeyValuePair<Tuple<string, HashSet<string>>, string>(
                    new Tuple<string, HashSet<string>>("s3:myprefix", new HashSet<string> {"hello"}),
                    @"{""s3:prefix"":[""hello"",""world""]}"),
                // Add existing k-v pair
                new KeyValuePair<Tuple<string, HashSet<string>>, string>(
                    new Tuple<string, HashSet<string>>("s3:prefix", new HashSet<string> {"hello"}),
                    @"{""s3:prefix"":[""world""]}"),
                // Add existing key and not value
                new KeyValuePair<Tuple<string, HashSet<string>>, string>(
                    new Tuple<string, HashSet<string>>("s3:prefix", new HashSet<string> {"world"}), @"{}")
            };
            var cmap = new ConditionKeyMap();
            cmap.Add("s3:prefix", new HashSet<string> {"hello", "world"});

            var index = 0;
            foreach (var pair in testCases)
            {
                try
                {
                    index += 1;
                    var testcase = pair.Key;
                    var prefix = testcase.Item1;
                    var stringSet = testcase.Item2;
                    var expectedConditionKMap = pair.Value;
                    cmap.Remove(prefix, stringSet);
                    var cmpstring = JsonConvert.SerializeObject(cmap, Formatting.None,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });

                    Assert.Equal(cmpstring, expectedConditionKMap);
                }
                catch (ArgumentException)
                {
                    Assert.NotEqual(index, 1);
                }
            }
        }

        [Fact]
        public void TestConditionMapAdd()
        {
            var cmap = new ConditionMap();

            var ckmap1 = new ConditionKeyMap("s3:prefix", "hello");
            var ckmap2 = new ConditionKeyMap("s3:prefix", new HashSet<string> {"hello", "world"});

            var testCases = new List<KeyValuePair<Tuple<string, ConditionKeyMap>, string>>
            {
                // Add new key and value
                new KeyValuePair<Tuple<string, ConditionKeyMap>, string>(Tuple.Create("StringEquals", ckmap1),
                    @"{""StringEquals"":{""s3:prefix"":[""hello""]}}"),
                //Add existing key and value
                new KeyValuePair<Tuple<string, ConditionKeyMap>, string>(Tuple.Create("StringEquals", ckmap1),
                    @"{""StringEquals"":{""s3:prefix"":[""hello""]}}"),
                //Add existing key and new value
                new KeyValuePair<Tuple<string, ConditionKeyMap>, string>(Tuple.Create("StringEquals", ckmap2),
                    @"{""StringEquals"":{""s3:prefix"":[""hello"",""world""]}}")
            };
            var index = 0;
            foreach (var pair in testCases)
            {
                var tuple = pair.Key;
                var expectedJSON = pair.Value;

                index += 1;
                cmap.Put(tuple.Item1, tuple.Item2);
                var cmapJSON = JsonConvert.SerializeObject(cmap, Formatting.None,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                Assert.Equal(expectedJSON, cmapJSON);
            }
        }

        [Fact]
        // Tests if condition key map merges existing values 
        public void TestConditionMapPutAll()
        {
            var cmap1 = new ConditionMap();
            cmap1.Add("StringEquals", new ConditionKeyMap("s3:prefix", new HashSet<string> {"hello"}));

            var cmap2 = new ConditionMap();
            cmap2.Add("StringEquals", new ConditionKeyMap("s3:prefix", new HashSet<string> {"world"}));

            var cmap3 = new ConditionMap();
            cmap3.Add("StringEquals", new ConditionKeyMap("s3:myprefix", new HashSet<string> {"world"}));

            var cmap4 = new ConditionMap();
            cmap4.Add("StringEquals", new ConditionKeyMap("s3:prefix", new HashSet<string> {"hello"}));
            var testCases = new List<KeyValuePair<Tuple<ConditionMap, ConditionMap>, string>>
            {
                // Both args are empty
                new KeyValuePair<Tuple<ConditionMap, ConditionMap>, string>(
                    Tuple.Create(new ConditionMap(), new ConditionMap()), @"{}"),
                // First arg empty
                new KeyValuePair<Tuple<ConditionMap, ConditionMap>, string>(Tuple.Create(new ConditionMap(), cmap1),
                    @"{""StringEquals"":{""s3:prefix"":[""hello""]}}"),
                //Second arg empty
                new KeyValuePair<Tuple<ConditionMap, ConditionMap>, string>(Tuple.Create(cmap1, new ConditionMap()),
                    @"{""StringEquals"":{""s3:prefix"":[""hello""]}}"),
                //Both args have same value
                new KeyValuePair<Tuple<ConditionMap, ConditionMap>, string>(Tuple.Create(cmap1, cmap4),
                    @"{""StringEquals"":{""s3:prefix"":[""hello""]}}"),
                //Value of second arg will be merged
                new KeyValuePair<Tuple<ConditionMap, ConditionMap>, string>(Tuple.Create(cmap1, cmap2),
                    @"{""StringEquals"":{""s3:prefix"":[""hello"",""world""]}}"),
                //second arg will be added 
                new KeyValuePair<Tuple<ConditionMap, ConditionMap>, string>(Tuple.Create(cmap1, cmap3),
                    @"{""StringEquals"":{""s3:prefix"":[""hello"",""world""],""s3:myprefix"":[""world""]}}")
            };

            var index = 0;
            foreach (var pair in testCases)
            {
                try
                {
                    index += 1;
                    var testcase = pair.Key;
                    var first = testcase.Item1;
                    var second = testcase.Item2;
                    var expectedConditionKMapJSON = pair.Value;
                    first.PutAll(second);
                    var cmpstring = JsonConvert.SerializeObject(first, Formatting.None,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });

                    Assert.Equal(cmpstring, expectedConditionKMapJSON);
                }
                catch (ArgumentException)
                {
                    throw;
                }
            }
        }


        [Fact]
        public void TestIfStringIsetGetsDeSerialized_Test1()
        {
            var policyString =
                @"{""Version"":""2012 - 10 - 17"",""Statement"":[{""Sid"":"""",""Effect"":""Allow"",""Principal"":{""AWS"":"" * ""},""Action"":""s3: GetBucketLocation"",""Resource"":""arn: aws: s3:::miniodotnetvpn5pic718xfutt""},{""Sid"":"""",""Effect"":""Allow"",""Principal"":{""AWS"":"" * ""},""Action"":""s3: ListBucket"",""Resource"":""arn: aws: s3:::miniodotnetvpn5pic718xfutt"",""Condition"":{""StringEquals"":{""s3: prefix"":""dotnetcms1ssazhd""}}},{""Sid"":"""",""Effect"":""Allow"",""Principal"":{""AWS"":"" * ""},""Action"":""s3: GetObject"",""Resource"":""arn: aws: s3:::miniodotnetvpn5pic718xfutt / dotnetcms1ssazhd * ""}]}";


            // ConditionKeyMap ckmap = JsonConvert.DeserializeObject<ConditionKeyMap>(ckmapString);
            var contentBytes = Encoding.UTF8.GetBytes(policyString);
            var bucketName = "miniodotnetvpn5pic718xfutt";
            var stream = new MemoryStream(contentBytes);
            var policy = BucketPolicy.ParseJson(stream, bucketName);
        }
    }
}