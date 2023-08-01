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

using CommunityToolkit.HighPerformance;

namespace Minio.Functional.Tests;

internal sealed class RandomStreamGenerator
{
    private readonly Random _random = new();
    private readonly Memory<byte> _seedBuffer;

    public RandomStreamGenerator(int maxBufferSize)
    {
        _seedBuffer = new byte[maxBufferSize];
#if NETFRAMEWORK
        _random.NextBytes(_seedBuffer.Span.ToArray());
#else
        _random.NextBytes(_seedBuffer.Span);
#endif
    }

    public Stream GenerateStreamFromSeed(int size)
    {
        var randomWindow = _random.Next(0, size);

        Memory<byte> buffer = new byte[size];

        _seedBuffer.Slice(randomWindow, size - randomWindow).CopyTo(buffer.Slice(0, size - randomWindow));
        _seedBuffer.Slice(0, randomWindow).CopyTo(buffer.Slice(size - randomWindow, randomWindow));

        return buffer.AsStream();
    }
}
