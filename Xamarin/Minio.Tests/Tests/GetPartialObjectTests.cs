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
    internal class GetPartialObjectTests : AbstractMinioTests
    {
        /// <summary>
        ///     Get object in a bucket for a particular offset range. Dotnet SDK currently
        ///     requires both start offset and end
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task HappyCase()
        {
            // arrange
            var basketName = await this.GetTargetBasketName();
            var fileName = await this.CreateFileForTarget();

            byte[] fileContent;
            try
            {
                // act
                // Get object content starting at byte position 1024 and length of 4096
                Console.WriteLine("Running example for API: GetObjectAsync");
                fileContent = await this.MinioClient.GetObjectAsync(basketName, fileName, 8L, 64L);
            }
            finally
            {
                // clean
                await this.RemoveFileForTarget(fileName);
            }

            // log
            Console.WriteLine("Successfully downloaded object with requested offset and length {0} into file",
                fileContent.Length);
        }
    }
}