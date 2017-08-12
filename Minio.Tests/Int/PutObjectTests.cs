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
    using System.IO;
    using System.Threading.Tasks;
    using Xunit;

    public class PutObjectTests : AbstractMinioTests
    {
        private const int MB = 1024 * 1024;

        /// <summary>
        ///     Put an large object from a local stream into bucket
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HappyLargeFileCase()
        {
            // arrange
            var basketName = await this.GetTargetBasketName();
            var fileName = this.GetRandomName();
            var fileContent = this.GetRandomFile(MB * 6);
            using (var filestream = new MemoryStream(fileContent))
            {
                try
                {
                    // act
                    Console.WriteLine("Running example for API: PutObjectAsync with Stream and MultiPartUpload");
                    await this.MinioClient.PutObjectAsync(basketName,
                        fileName,
                        filestream,
                        "application/octet-stream");
                }
                finally
                {
                    // clean
                    fileContent = null;
                    await this.RemoveFileForTarget(fileName);
                }

                // assert
                Assert.NotEmpty(fileName);

                // log
                Console.WriteLine("Uploaded object " + fileName + " to bucket " + TargetBasketName);
            }
        }

        /// <summary>
        ///     Put an small object from a local stream into bucket
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HappySimpleFileCase()
        {
            // arrange
            var randomFileName = this.GetRandomName();
            var randomFileContent = this.GetRandomFile();

            try
            {
                // act
                Console.WriteLine("Running example for API: PutObjectAsync with FileName");
                randomFileName = await this.CreateFileForTarget(randomFileName, randomFileContent);
            }
            finally
            {
                // clean
                await this.RemoveFileForTarget(randomFileName);
            }

            // assert
            Assert.NotEmpty(randomFileName);
            Assert.NotNull(randomFileContent);

            // log
            Console.WriteLine("Uploaded object " + randomFileName + " to bucket " + TargetBasketName);
        }
    }
}