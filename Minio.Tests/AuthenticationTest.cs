/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.Exceptions;

namespace Minio.Tests
{
    [TestClass]
    public class AuthenticationTest
    {
        private const string AssumeRoleAccessKey = "Q3AM3UQ867SPQQA43P2F-assumerole";
        private const string AssumeRoleSecretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG-assumerole";
        private const string PlayMinioEndpoint = "play.min.io";

        private static async Task PutBlobAsync(MinioClient minioClient, string bucketName, string blobName, string data)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                await minioClient.PutObjectAsync(bucketName, blobName, stream, stream.Length);
            }
        }

        private static async Task AssertBlobAsync(MinioClient minioClient, string bucketName, string blobName, string data)
        {
            string dataFromStorage = null;
            await minioClient.GetObjectAsync(
                bucketName,
                blobName,
                s =>
                {
                        using (var reader = new StreamReader(s, Encoding.UTF8, true))
                        {
                            dataFromStorage = reader.ReadToEnd();
                        }
                });

            Assert.AreEqual(data, dataFromStorage);
        }

        private static async Task AssertBlobForbiddenAsync(MinioClient minioClient, string bucketName, string blobName)
        {
            await Assert.ThrowsExceptionAsync<ForbiddenException>(
                () => minioClient.GetObjectAsync(
                    bucketName,
                    blobName,
                    s => { }));
            await Assert.ThrowsExceptionAsync<ForbiddenException>(
                () => PutBlobAsync(minioClient, bucketName, blobName, "-"));

            await Assert.ThrowsExceptionAsync<ForbiddenException>(
                () => minioClient.RemoveObjectAsync(bucketName, blobName));
        }

        private static async Task AssertBlobReadOnlyAsync(MinioClient minioClient, string bucketName, string blobName, string data)
        {
            await AssertBlobAsync(minioClient, bucketName, blobName, data);
            await Assert.ThrowsExceptionAsync<ForbiddenException>(
                () => PutBlobAsync(minioClient, bucketName, blobName, "-"));
            await Assert.ThrowsExceptionAsync<ForbiddenException>(
                () => minioClient.RemoveObjectAsync(bucketName, blobName));
        }

        private static async Task InitBucketAsync(MinioClient minioClient, string bucketName, string blobName, string data)
        {
            if (!await minioClient.BucketExistsAsync(bucketName))
                await minioClient.MakeBucketAsync(bucketName);

            await PutBlobAsync(minioClient, bucketName, blobName, data);

            await AssertBlobAsync(minioClient, bucketName, blobName, data);
        }

        private static MinioClient CreateMinioClient()
        {
            var minioClient = new MinioClient(PlayMinioEndpoint, AssumeRoleAccessKey, AssumeRoleSecretKey).WithSSL();
            return minioClient;
        }

        [TestMethod]
        public async Task AssumeRole_Ok()
        {
            const string data = "Hello, world!";
            var minioClient = CreateMinioClient();

            var bucketName = Guid.NewGuid().ToString("N");
            var prefix = "path/";
            var blobName = prefix + Guid.NewGuid();
            try
            {
                await InitBucketAsync(minioClient, bucketName, blobName, data);

                // Get assume role
                var assumeTtl = TimeSpan.FromMinutes(42);
                var assume = await minioClient.AssumeRoleAsync(duration: assumeTtl);
                Assert.AreEqual(string.Empty, assume.AssumedRoleUser.Arn);
                Assert.AreEqual(string.Empty, assume.AssumedRoleUser.AssumeRoleId);
                Assert.IsTrue(assume.Credentials.Expiration - DateTime.UtcNow - assumeTtl < TimeSpan.FromMinutes(1));

                // Read with assume role
                minioClient.WithCredentials(assume);
                await AssertBlobAsync(minioClient, bucketName, blobName, data);

                // Cannot assume
                await Assert.ThrowsExceptionAsync<ForbiddenException>(() => minioClient.AssumeRoleAsync());
            }
            finally
            {
                minioClient.WithCredentials(AssumeRoleAccessKey, AssumeRoleSecretKey);
                await minioClient.RemoveObjectAsync(bucketName, blobName);
                await minioClient.RemoveBucketAsync(bucketName);
            }
        }

        [TestMethod]
        public async Task AssumeRolePrefix_Ok()
        {
            const string data = "Hello, world!";

            var minioClient = CreateMinioClient();

            var bucketName = Guid.NewGuid().ToString("N");
            var bucketNameOther = Guid.NewGuid().ToString("N");
            var prefixA = "pathA/";
            var prefixB = "pathB/";
            var blobNameA = prefixA + Guid.NewGuid();
            var blobNameB = prefixB + Guid.NewGuid();

            try
            {
                await InitBucketAsync(minioClient, bucketName, blobNameA, data + blobNameA);
                await InitBucketAsync(minioClient, bucketName, blobNameB, data + blobNameB);

                var assumeNoPermission = await minioClient.AssumeRoleAsync(
                    MinioClientHelper.GetBucketPolicy(bucketNameOther));
                var assumePrefixA = await minioClient.AssumeRoleAsync(
                    MinioClientHelper.GetBucketPolicy(bucketName, prefixA));
                var assumePrefixAReadOnly = await minioClient.AssumeRoleAsync(
                    MinioClientHelper.GetBucketPolicy(bucketName, prefixA, true));
                var assumePrefixB = await minioClient.AssumeRoleAsync(
                    MinioClientHelper.GetBucketPolicy(bucketName, prefixB));
                var assumePrefixBReadOnly = await minioClient.AssumeRoleAsync(
                    MinioClientHelper.GetBucketPolicy(bucketName, prefixB, true));

                // Assume other bucket
                minioClient.WithCredentials(assumeNoPermission);
                await AssertBlobForbiddenAsync(minioClient, bucketName, blobNameA);
                await AssertBlobForbiddenAsync(minioClient, bucketName, blobNameB);

                // Assume prefix A
                minioClient.WithCredentials(assumePrefixA);
                await AssertBlobAsync(minioClient, bucketName, blobNameA, data + blobNameA);
                await AssertBlobForbiddenAsync(minioClient, bucketName, blobNameB);
                await AssertBlobForbiddenAsync(minioClient, bucketNameOther, blobNameA);

                // Assume prefix A (read-only)
                minioClient.WithCredentials(assumePrefixAReadOnly);
                await AssertBlobReadOnlyAsync(minioClient, bucketName, blobNameA, data + blobNameA);
                await AssertBlobForbiddenAsync(minioClient, bucketName, blobNameB);
                await AssertBlobForbiddenAsync(minioClient, bucketNameOther, blobNameA);

                // Assume prefix B
                minioClient.WithCredentials(assumePrefixB);
                await AssertBlobForbiddenAsync(minioClient, bucketName, blobNameA);
                await AssertBlobAsync(minioClient, bucketName, blobNameB, data + blobNameB);
                await AssertBlobForbiddenAsync(minioClient, bucketNameOther, blobNameB);

                // Assume prefix B (read-only)
                minioClient.WithCredentials(assumePrefixBReadOnly);
                await AssertBlobForbiddenAsync(minioClient, bucketName, blobNameA);
                await AssertBlobReadOnlyAsync(minioClient, bucketName, blobNameB, data + blobNameB);
                await AssertBlobForbiddenAsync(minioClient, bucketNameOther, blobNameB);
            }
            finally
            {
                minioClient.WithCredentials(AssumeRoleAccessKey, AssumeRoleSecretKey);
                await minioClient.RemoveObjectAsync(bucketName, blobNameA);
                await minioClient.RemoveObjectAsync(bucketName, blobNameB);
                await minioClient.RemoveBucketAsync(bucketName);
            }
        }
    }
}