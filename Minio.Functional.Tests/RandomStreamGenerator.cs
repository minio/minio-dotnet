/*
* MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
* (C) 2017, 2018, 2019 MinIO, Inc.
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

namespace Minio.Functional.Tests
{
    internal class RandomStreamGenerator
    {
        private readonly Random _random = new Random();
        private readonly byte[] _seedBuffer;

        public RandomStreamGenerator(int maxBufferSize)
        {
            this._seedBuffer = new byte[maxBufferSize];
            this._random.NextBytes(this._seedBuffer);
        }

        public MemoryStream GenerateStreamFromSeed(int size)
        {
            int randomWindow = this._random.Next(0, size);

            byte[] buffer = new byte[size];

            Buffer.BlockCopy(this._seedBuffer, randomWindow, buffer, 0, size - randomWindow);
            Buffer.BlockCopy(this._seedBuffer, 0, buffer, size - randomWindow, randomWindow);

            return new MemoryStream(buffer);
        }
    }
}
