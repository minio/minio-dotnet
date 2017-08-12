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

    public class ListIncompleteUploadsTests : AbstractMinioTests
    {
        /// <summary>
        ///     List incomplete uploads on the bucket matching specified prefix
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HappyCase()
        {
            // arrange
            var basketName = await this.GetTargetBasketName();

            // act
            Console.Out.WriteLine("Running example for API: ListIncompleteUploads");
            var uploads = await this.MinioClient.ListIncompleteUploads(basketName, "", true);

            // assert
            Assert.NotNull(uploads);

            // log
            foreach (var upload in uploads)
            {
                Console.WriteLine("OnNext: {0}", upload.Key);
            }

            Console.WriteLine("Listed the pending uploads to bucket " + TargetBasketName);
        }
    }
}