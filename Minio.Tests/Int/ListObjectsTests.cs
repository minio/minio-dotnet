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

    public class ListObjectsTests : AbstractMinioTests
    {
        /// <summary>
        ///     List objects matching optional prefix in a specified bucket.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HappyCase()
        {
            // arrange
            var basketName = await this.GetTargetBasketName();
            var objects = await this.MinioClient.ListObjectsAsync(basketName, null, false);

            // assert
            Assert.NotNull(objects);

            // log
            foreach (var item in objects)
            {
                Console.WriteLine("Object: {0}", item.Key);
            }

            Console.WriteLine("Listed all objects in bucket " + TargetBasketName + "\n");
        }
    }
}