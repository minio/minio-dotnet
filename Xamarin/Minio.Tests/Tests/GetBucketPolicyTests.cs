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
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    internal class GetBucketPolicyTests : AbstractMinioTests
    {
        /// <summary>
        ///     Get bucket policy
        /// </summary>
        /// <returns></returns>
		[Test]
        public async Task HappyCase()
        {
            // arrange & act
            var basketName = await this.GetTargetBasketName();
            Console.WriteLine("Running example for API: GetPolicyAsync");
            var policy = await this.MinioClient.GetPolicyAsync(basketName);

            // assert
            Assert.NotNull(policy);

            // log
            Console.WriteLine("Current Policy is " + policy + " for bucket " + TargetBasketName);
        }
    }
}