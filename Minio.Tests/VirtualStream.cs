/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2017-2021 MinIO, Inc.
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


using Minio.DataModel.Tags;
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;

namespace Minio.Tests;

/// <summary>
/// Virtural stream will create a random data stream up to the user's specific size.
/// It will not allocate any data. This stream can only be read forward.
/// </summary>
public class VirtualStream : Stream
{
    /// <summary>
    /// The current position in the stream
    /// </summary>
    private long _position = 0;

    /// <summary>
    /// The total size of data to produce.
    /// </summary>
    private readonly long _size;

    /// <summary>
    /// Random generator for data.
    /// </summary>
    private readonly Random _random;

    public VirtualStream(long size) : this(size, Random.Shared)
    {
    }

    public VirtualStream(long size, Random random)
    {
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater or equal to zero");

        _random = random ?? throw new ArgumentNullException(nameof(random));
        _size = size;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => _position; set => throw new NotSupportedException(); }
    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        // if there is enough data to return count bytes, then return that amount
        // otherwise return what ever is left over.
        int bytes = ((_size - _position) >= count)
            ? count
            : (int)(_size - _position);

        if (bytes != 0)
        {
            var span = buffer.AsSpan(offset, bytes);
            _random.NextBytes(span);

            _position += bytes;
        }

        return bytes;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
