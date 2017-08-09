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

namespace Minio.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Minio.DataModel;
    using NUnit.Framework;

    [TestFixture]
    public class PresignedPostPolicyTests : AbstractMinioTests
    {
        [Test]
        public async Task HappyCase()
        {
            // arrange
            var form = new PostPolicy();
            var expiration = DateTime.UtcNow;
            var basketName = await this.GetTargetBasketName();
            var fileName = await this.CreateFileForTarget();
            form.SetExpires(expiration.AddDays(10));
            form.SetKey(fileName);
            form.SetBucket(basketName);

            string curlCommand;
            try
            {
                // act
                var tuple = await this.MinioClient.PresignedPostPolicyAsync(form);
                curlCommand =
                    tuple.Item2.Aggregate("curl ", (current, pair) => current + $"-F {pair.Key}={pair.Value}");
                curlCommand += " -F file=@/etc/bashrc " + tuple.Item1; // https://s3.amazonaws.com/my-bucketname";
            }
            finally
            {
                // clean
                await this.RemoveFileForTarget(fileName);
            }

            // assert
            Assert.NotNull(curlCommand);

            // log
            Console.WriteLine(curlCommand);
        }
    }
}