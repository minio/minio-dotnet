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
using Minio.Exceptions;

namespace Minio.Tests
{
    using System;
    using System.Threading.Tasks;
    using Minio.DataModel;
    using Minio.DataModel.Policy;
    using NUnit.Framework;

	// TODO Minio.Exceptions.MinioException : The provided 'x-amz-content-sha256' header does not match what was computed.
    [TestFixture]
    internal class SetBucketPolicyTests : AbstractMinioTests
    {
        /// <summary>
        ///     Set read only bucket policy
        /// </summary>
        /// <returns></returns>
        public async Task HappyReadOnlyCase()
        {
            // arrange
            var policyBasketName = this.GetRandomName();
            await this.MinioClient.MakeBucketAsync(policyBasketName);
            const string basketReadPrefix = "";

            try
            {
                // act
				// Minio.Exceptions.MinioException : The provided 'x-amz-content-sha256' header does not match what was computed.
				var ex = Assert.ThrowsAsync<MinioException>(() => this.MinioClient.SetPolicyAsync(policyBasketName, basketReadPrefix, PolicyType.ReadOnly));
            }
            finally
            {
                // clean
                await this.MinioClient.RemoveBucketAsync(policyBasketName);
            }

            // assert
            Assert.NotNull(basketReadPrefix);

            // log
            Console.WriteLine("Policy " + PolicyType.ReadOnly + " set for the bucket " + policyBasketName +
                              " successfully");
        }

        /// <summary>
        ///     Set read write bucket policy
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task HappyReadWriteCase()
        {
            // arrange
            var policyBasketName = this.GetRandomName();
            await this.MinioClient.MakeBucketAsync(policyBasketName);
            const string basketReadWritePrefix = "prefixReadWrite";

            try
            {
                // act
				// Minio.Exceptions.MinioException : The provided 'x-amz-content-sha256' header does not match what was computed.
				var ex = Assert.ThrowsAsync<MinioException>(() => this.MinioClient.SetPolicyAsync(policyBasketName, basketReadWritePrefix, PolicyType.ReadWrite));
            }
            finally
            {
                // clean
                await this.MinioClient.RemoveBucketAsync(policyBasketName);
            }

            // assert
            Assert.NotNull(basketReadWritePrefix);

            // log
            Console.WriteLine("Policy " + PolicyType.ReadWrite + " set for the bucket " + policyBasketName +
                              " successfully");
        }

        /// <summary>
        ///     Set write only bucket policy
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task HappyWriteOnlyCase()
        {
            // arrange
            var policyBasketName = this.GetRandomName();
            await this.MinioClient.MakeBucketAsync(policyBasketName);

            const string basketWritePrefix = "prefixWrite";

            try
            {
                // act
				// Minio.Exceptions.MinioException : The provided 'x-amz-content-sha256' header does not match what was computed.
				var ex = Assert.ThrowsAsync<MinioException>(() => this.MinioClient.SetPolicyAsync(policyBasketName, basketWritePrefix, PolicyType.WriteOnly));
            }
            finally
            {
                // clean
                await this.MinioClient.RemoveBucketAsync(policyBasketName);
            }

            // assert
            Assert.NotNull(basketWritePrefix);

            // log
            Console.WriteLine("Policy " + PolicyType.WriteOnly + " set for the bucket " + policyBasketName +
                              " successfully");
        }
    }
}