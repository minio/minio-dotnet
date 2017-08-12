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
    using Xunit;

    public class CopyObjectTests : AbstractMinioTests
    {
        /// <summary>
        ///     Copy object from one bucket to another
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HappyCase()
        {
            // arrange
            var fromBucketName = await this.GetTargetBasketName();
            var fromObjectName = await this.CreateFileForTarget();
            var destBucketName = await this.GetSpareBasketName();
            var destObjectName = this.GetRandomName();

            try
            {
                // act
                Console.WriteLine("Running example for API: CopyObjectAsync");
                // Optionally pass copy conditions
                await this.MinioClient.CopyObjectAsync(fromBucketName,
                    fromObjectName,
                    destBucketName,
                    destObjectName);
            }
            finally
            {
                // clean
                await this.RemoveFileForTarget(fromObjectName);
                await this.RemoveFileForSpare(destObjectName);
            }

            // assert
            Assert.NotEmpty(fromBucketName);
            Assert.NotEmpty(fromObjectName);
            Assert.NotEmpty(destBucketName);
            Assert.NotEmpty(destObjectName);

            // log
            Console.WriteLine("Copied object {0} from bucket {1} to bucket {2}", fromObjectName, fromBucketName,
                destBucketName);
        }
    }
}