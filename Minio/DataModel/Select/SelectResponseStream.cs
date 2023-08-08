using System.IO.Hashing;
using System.Text;
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
using Minio.Exceptions;
using Minio.Helper;

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

namespace Minio.DataModel.Select;

[Serializable]
public sealed class SelectResponseStream : IDisposable
{
    private readonly Memory<byte> messageCRC = new byte[4];
    private readonly MemoryStream payloadStream;
    private readonly Memory<byte> prelude = new byte[8];
    private readonly Memory<byte> preludeCRC = new byte[4];
    private bool disposed;

    private bool isProcessing;

    public SelectResponseStream()
    {
    }

    // SelectResponseStream is a struct for selectobjectcontent response.
    public SelectResponseStream(Stream stream)
    {
        if (stream is not null)
        {
            var _ms = new MemoryStream();
            stream.CopyTo(_ms);
            payloadStream = _ms;
            Payload = new MemoryStream();
        }

        isProcessing = true;
        _ = payloadStream.Seek(0, SeekOrigin.Begin);
        Start();
    }

    public Stream Payload { get; private set; }

    [XmlElement("Stats", IsNullable = false)]
    public StatsMessage Stats { get; set; }

    [XmlElement("Progress", IsNullable = false)]
    public ProgressMessage Progress { get; set; }

    public void Dispose()
    {
        if (disposed) return;

        payloadStream?.Dispose();
        Payload?.Dispose();

        Payload = null;

        disposed = true;
    }

    private int ReadFromStream(Span<byte> buffer)
    {
        var read = -1;
        if (!isProcessing) return read;

#if NETSTANDARD
        var bytes = new byte[buffer.Length];
        read
            = payloadStream.Read(bytes, 0, buffer.Length);
        bytes.CopyTo(buffer);
#else
        read = payloadStream.Read(buffer);
#endif
        if (!payloadStream.CanRead) isProcessing = false;
        return read;
    }

    private void Start()
    {
        var numBytesRead = 0;
        while (isProcessing)
        {
            var n = ReadFromStream(prelude.Span);
            numBytesRead += n;
            n = ReadFromStream(preludeCRC.Span);
            Span<byte> preludeCRCBytes = new byte[preludeCRC.Length];
            preludeCRC.Span.CopyTo(preludeCRCBytes);
            if (BitConverter.IsLittleEndian) preludeCRCBytes.Reverse();
            numBytesRead += n;
            Span<byte> inputArray = new byte[prelude.Length + 4];
            prelude.Span.CopyTo(inputArray[..prelude.Length]);

            var destinationPrelude = inputArray.Slice(inputArray.Length - 4, 4);
            var isValidPrelude = Crc32.TryHash(inputArray[..^4], destinationPrelude, out _);
            if (!isValidPrelude) throw new ArgumentException("invalid prelude CRC", nameof(destinationPrelude));

            if (!destinationPrelude.SequenceEqual(preludeCRCBytes))
                throw new ArgumentException("Prelude CRC Mismatch", nameof(preludeCRCBytes));

            var preludeBytes = prelude[..4].Span;
            Span<byte> bytes = new byte[preludeBytes.Length];
            preludeBytes.CopyTo(bytes);
            if (BitConverter.IsLittleEndian) bytes.Reverse();

#if NETSTANDARD
            var totalLength = BitConverter.ToInt32(bytes.ToArray(), 0);
#else
            var totalLength = BitConverter.ToInt32(bytes);
#endif
            preludeBytes = prelude.Slice(4, 4).Span;
            bytes = new byte[preludeBytes.Length];
            preludeBytes.CopyTo(bytes);
            if (BitConverter.IsLittleEndian) bytes.Reverse();

#if NETSTANDARD
            var headerLength = BitConverter.ToInt32(bytes.ToArray(), 0);
#else
            var headerLength = BitConverter.ToInt32(bytes);
#endif
            var payloadLength = totalLength - headerLength - 16;

            Span<byte> headers = new byte[headerLength];
            Memory<byte> payload = new byte[payloadLength];
            var num = ReadFromStream(headers);
            if (num != headerLength) throw new IOException("insufficient data");
            num = ReadFromStream(payload.Span);
            if (num != payloadLength) throw new IOException("insufficient data");

            numBytesRead += num;
            num = ReadFromStream(messageCRC.Span);

            var messageBytes = messageCRC.Span;
            Span<byte> messageCRCBytes = new byte[messageBytes.Length];
            messageBytes.CopyTo(messageCRCBytes);
            if (BitConverter.IsLittleEndian) messageCRCBytes.Reverse();
            // now verify message CRC
            inputArray = new byte[totalLength];

            prelude.Span.CopyTo(inputArray);
            preludeCRC.Span.CopyTo(inputArray.Slice(prelude.Length, preludeCRC.Length));
            headers.CopyTo(inputArray.Slice(prelude.Length + preludeCRC.Length, headerLength));
            payload.Span.CopyTo(inputArray.Slice(prelude.Length + preludeCRC.Length + headerLength, payloadLength));

            var destinationMessage = inputArray.Slice(inputArray.Length - 4, 4);
            var isValidMessage = Crc32.TryHash(inputArray[..^4], destinationMessage, out _);
            if (!isValidMessage) throw new ArgumentException("invalid message CRC", nameof(destinationMessage));

            if (!destinationMessage.SequenceEqual(messageCRCBytes))
                throw new ArgumentException("message CRC Mismatch", nameof(messageCRCBytes));

            var headerMap = ExtractHeaders(headers);

            if (headerMap.TryGetValue(":message-type", out var value))
                if (value.Equals(":error", StringComparison.OrdinalIgnoreCase))
                {
                    headerMap.TryGetValue(":error-code", out var errorCode);
                    headerMap.TryGetValue(":error-message", out var errorMessage);
                    throw new SelectObjectContentException(errorCode + ":" + errorMessage);
                }

            if (headerMap.TryGetValue(":event-type", out value))
            {
                if (value.Equals("End", StringComparison.OrdinalIgnoreCase))
                {
                    // throw new UnexpectedShortReadException("Insufficient data");
                    isProcessing = false;
                    break;
                }

                if (value.Equals("Cont", StringComparison.OrdinalIgnoreCase) || payloadLength < 1) continue;
                if (value.Equals("Progress", StringComparison.OrdinalIgnoreCase))
                {
                    var progress = new ProgressMessage();
                    using var stream = payload.AsStream();
                    progress = Utils.DeserializeXml<ProgressMessage>(stream);

                    Progress = progress;
                }

                if (value.Equals("Stats", StringComparison.OrdinalIgnoreCase))
                {
                    var stats = new StatsMessage();
                    using var stream = payload.AsStream();
                    stats = Utils.DeserializeXml<StatsMessage>(stream);

                    Stats = stats;
                }

#if NETSTANDARD
                if (value.Equals("Records", StringComparison.OrdinalIgnoreCase))
                    Payload.Write(payload.ToArray(), 0, payloadLength);
#else
                if (value.Equals("Records", StringComparison.OrdinalIgnoreCase)) Payload.Write(payload.Span);
#endif
            }
        }

        isProcessing = false;
        Payload.Seek(0, SeekOrigin.Begin);
        payloadStream.Close();
    }

    private IDictionary<string, string> ExtractHeaders(Span<byte> data)
    {
        var headerMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var offset = 0;

        while (offset < data.Length)
        {
            var nameLength = data[offset++];
            var b = data.Slice(offset, nameLength);

            var name = Encoding.UTF8.GetString(b);

            offset += nameLength;
            var hdrValue = data[offset++];
            if (hdrValue != 7) throw new IOException("header value type is not 7");
            b = data.Slice(offset, 2);
            if (BitConverter.IsLittleEndian) b.Reverse();
            offset += 2;

#if NETSTANDARD
            int headerValLength = BitConverter.ToInt16(b.ToArray(), 0);
#else
            int headerValLength = BitConverter.ToInt16(b);
#endif
            b = data.Slice(offset, headerValLength);

            var value = Encoding.UTF8.GetString(b);
            offset += headerValLength;
            headerMap.Add(name, value);
        }

        return headerMap;
    }
}
