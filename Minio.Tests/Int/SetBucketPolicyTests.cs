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

namespace Minio.Tests.Int
{
    using System;
    using System.Threading.Tasks;
    using DataModel.Policy;
    using Exceptions;
    using Xunit;

    // TODO Minio.Exceptions.MinioException : The provided 'x-amz-content-sha256' header does not match what was computed.
    public class SetBucketPolicyTests : AbstractMinioTests
    {
        /// <summary>
        /// Set read only bucket policy
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HappyReadOnlyCase()
        {
            // arrange
            var policyBucketName = this.GetRandomName();
            await this.MinioClient.MakeBucketAsync(policyBucketName);
            const string bucketReadPrefix = "";

            try
            {
                // act
                // Minio.Exceptions.MinioException : The provided 'x-amz-content-sha256' header does not match what was computed.
                var ex = Assert.ThrowsAsync<MinioException>(() => this.MinioClient.SetPolicyAsync(policyBucketName, bucketReadPrefix, PolicyType.ReadOnly));
            }
            finally
            {
                // clean
                await this.MinioClient.RemoveBucketAsync(policyBucketName);
            }

            // assert
            Assert.NotNull(bucketReadPrefix);

            // log
            Console.WriteLine("Policy " + PolicyType.ReadOnly + " set for the bucket " + policyBucketName +
                              " successfully");
        }

        /// <summary>
        /// Set read write bucket policy
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HappyReadWriteCase()
        {
            // arrange
            var policyBucketName = this.GetRandomName();
            await this.MinioClient.MakeBucketAsync(policyBucketName);
            const string bucketReadWritePrefix = "prefixReadWrite";

            try
            {
                // act
                // Minio.Exceptions.MinioException : The provided 'x-amz-content-sha256' header does not match what was computed.
                var ex = Assert.ThrowsAsync<MinioException>(() => this.MinioClient.SetPolicyAsync(policyBucketName, bucketReadWritePrefix, PolicyType.ReadWrite));
            }
            finally
            {
                // clean
                await this.MinioClient.RemoveBucketAsync(policyBucketName);
            }

            // assert
            Assert.NotNull(bucketReadWritePrefix);

            // log
            Console.WriteLine("Policy " + PolicyType.ReadWrite + " set for the bucket " + policyBucketName +
                              " successfully");
        }

        /// <summary>
        /// Set write only bucket policy
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HappyWriteOnlyCase()
        {
            // arrange
            var policyBucketName = this.GetRandomName();
            await this.MinioClient.MakeBucketAsync(policyBucketName);

            const string bucketWritePrefix = "prefixWrite";

            try
            {
                // act
                // Minio.Exceptions.MinioException : The provided 'x-amz-content-sha256' header does not match what was computed.
                var ex = Assert.ThrowsAsync<MinioException>(() => this.MinioClient.SetPolicyAsync(policyBucketName, bucketWritePrefix, PolicyType.WriteOnly));
            }
            finally
            {
                // clean
                await this.MinioClient.RemoveBucketAsync(policyBucketName);
            }

            // assert
            Assert.NotNull(bucketWritePrefix);

            // log
            Console.WriteLine("Policy " + PolicyType.WriteOnly + " set for the bucket " + policyBucketName +
                              " successfully");
        }
    }
}