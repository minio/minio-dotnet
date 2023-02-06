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


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Minio.Tests;

/// <summary>
/// Tests to ensure the VirutalStream is working correct.
/// Other tests will use the VirutalStream to test uploading
/// huge documents to minio/S3.
/// </summary>
[TestClass]
public class VirutalStreamTest
{
    [TestMethod]
    public void zero_size_stream_reads_no_data()
    {
        byte[] buffer = new byte[1024];

        VirutalStream sut = new VirutalStream(0);

        var actual = sut.Read(buffer, 0, buffer.Length);

        Assert.AreEqual(0, actual);
        Assert.AreEqual(0, sut.Position);
    }

    [TestMethod]
    public void can_read_complete_buffer()
    {
        byte[] buffer = new byte[1024];

        VirutalStream sut = new VirutalStream(buffer.LongLength);

        var actual = sut.Read(buffer, 0, buffer.Length);
        Assert.AreEqual(buffer.Length, actual);

        // should not read any data this time
        actual = sut.Read(buffer, 0, buffer.Length);
        Assert.AreEqual(0, actual);
        Assert.AreEqual(buffer.LongLength, sut.Position);
    }

    [TestMethod]
    public void should_read_complete_stream()
    {
        // this test want to ensure the buffer size and stream size are not multiples of each other
        byte[] buffer = new byte[128];

        VirutalStream sut = new VirutalStream(135);

        // should read the complete buffer
        var actual = sut.Read(buffer, 0, buffer.Length);
        Assert.AreEqual(buffer.Length, actual);

        // read only the remaining data (135 - 128)
        actual = sut.Read(buffer, 0, buffer.Length);
        Assert.AreEqual(135 - 128, actual);

        // shouldnt get any more data
        actual = sut.Read(buffer, 0, buffer.Length);
        Assert.AreEqual(0, actual);
    }

    [TestMethod]
    public void should_read_into_correct_offset()
    {
        // this test want to ensure the buffer size and stream size are not multiples of each other
        byte[] buffer = new byte[128];

        VirutalStream sut = new VirutalStream(1024);

        // should read the complete buffer
        var actual = sut.Read(buffer, buffer.Length / 2, buffer.Length  / 2);
        Assert.AreEqual(buffer.Length / 2, actual);

        // first half of the buffer should still be zeros
        for (int i = 0; i < buffer.Length / 2; i++)
        {
            Assert.AreEqual(0, buffer[i]);
        }
    }
}