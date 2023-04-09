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

using System.IO.Hashing;
using System.Text;
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
using Minio.Exceptions;

namespace Minio.DataModel;

[Serializable]
public class SelectResponseStream
{
    private readonly Memory<byte> messageCRC = new byte[4];
    private readonly MemoryStream payloadStream;
    private readonly Memory<byte> prelude = new byte[8];
    private readonly Memory<byte> preludeCRC = new byte[4];

    private bool _isProcessing;

    public SelectResponseStream()
    {
    }

    // SelectResponseStream is a struct for selectobjectcontent response.
    public SelectResponseStream(Stream stream)
    {
        if (stream != null)
        {
            var _ms = new MemoryStream();
            stream.CopyTo(_ms);
            payloadStream = _ms;
            Payload = new MemoryStream();
        }

        _isProcessing = true;
        payloadStream.Seek(0, SeekOrigin.Begin);
        Start();
    }

    public Stream Payload { get; set; }

    [XmlElement("Stats", IsNullable = false)]
    public StatsMessage Stats { get; set; }

    [XmlElement("Progress", IsNullable = false)]
    public ProgressMessage Progress { get; set; }

    protected int ReadFromStream(Span<byte> buffer)
    {
        var read = -1;
        if (!_isProcessing) return read;

#if NETSTANDARD
        read = payloadStream.Read(buffer.ToArray(), 0, buffer.Length);
#else
        read = payloadStream.Read(buffer);
#endif
        if (!payloadStream.CanRead) _isProcessing = false;
        return read;
    }

    private void Start()
    {
        var numBytesRead = 0;
        while (_isProcessing)
        {
            var n = ReadFromStream(prelude.Span);
            numBytesRead += n;
            n = ReadFromStream(preludeCRC.Span);
            Span<byte> preludeCRCBytes = preludeCRC.ToArray();
            if (BitConverter.IsLittleEndian) preludeCRCBytes.Reverse();
            numBytesRead += n;
            Span<byte> inputArray = new byte[prelude.Length + 4];
            prelude.Span.CopyTo(inputArray.Slice(0, prelude.Length));

            var destinationPrelude = inputArray.Slice(inputArray.Length - 4, 4);
            var isValidPrelude = Crc32.TryHash(inputArray.Slice(0, inputArray.Length - 4), destinationPrelude, out _);
            if (!isValidPrelude) throw new ArgumentException("invalid prelude CRC");

            if (!destinationPrelude.SequenceEqual(preludeCRCBytes))
                throw new ArgumentException("Prelude CRC Mismatch");

            Span<byte> bytes = prelude.Slice(0, 4).ToArray();
            if (BitConverter.IsLittleEndian) bytes.Reverse();

#if NETSTANDARD
            var totalLength = BitConverter.ToInt32(bytes.ToArray(), 0);
#else
            var totalLength = BitConverter.ToInt32(bytes);
#endif
            bytes = prelude.Slice(4, 4).ToArray();
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
            Span<byte> messageCRCBytes = messageCRC.ToArray();
            if (BitConverter.IsLittleEndian) messageCRCBytes.Reverse();
            // now verify message CRC
            inputArray = new byte[totalLength];

            prelude.Span.CopyTo(inputArray);
            preludeCRC.Span.CopyTo(inputArray.Slice(prelude.Length, preludeCRC.Length));
            headers.CopyTo(inputArray.Slice(prelude.Length + preludeCRC.Length, headerLength));
            payload.Span.CopyTo(inputArray.Slice(prelude.Length + preludeCRC.Length + headerLength, payloadLength));

            var destinationMessage = inputArray.Slice(inputArray.Length - 4, 4);
            var isValidMessage = Crc32.TryHash(inputArray.Slice(0, inputArray.Length - 4), destinationMessage, out _);
            if (!isValidMessage) throw new ArgumentException("invalid message CRC");

            if (!destinationMessage.SequenceEqual(messageCRCBytes))
                throw new ArgumentException("message CRC Mismatch");

            var headerMap = ExtractHeaders(headers);

            if (headerMap.TryGetValue(":message-type", out var value))
                if (value.Equals(":error"))
                {
                    headerMap.TryGetValue(":error-code", out var errorCode);
                    headerMap.TryGetValue(":error-message", out var errorMessage);
                    throw new SelectObjectContentException(errorCode + ":" + errorMessage);
                }

            if (headerMap.TryGetValue(":event-type", out value))
            {
                if (value.Equals("End"))
                {
                    // throw new UnexpectedShortReadException("Insufficient data");
                    _isProcessing = false;
                    break;
                }

                if (value.Equals("Cont") || payloadLength < 1) continue;
                if (value.Equals("Progress"))
                {
                    var progress = new ProgressMessage();

                        progress = (ProgressMessage)new XmlSerializer(typeof(ProgressMessage)).Deserialize(payload.AsStream());

                    Progress = progress;
                }

                if (value.Equals("Stats"))
                {
                    var stats = new StatsMessage();

                        stats = (StatsMessage)new XmlSerializer(typeof(StatsMessage)).Deserialize(payload.AsStream());

                    Stats = stats;
                }

#if NETSTANDARD
                if (value.Equals("Records")) Payload.Write(payload.ToArray(), 0, payloadLength);
#else
                if (value.Equals("Records")) Payload.Write(payload.Span);
#endif
            }
        }

        _isProcessing = false;
        Payload.Seek(0, SeekOrigin.Begin);
        payloadStream.Close();
    }

    protected Dictionary<string, string> ExtractHeaders(Span<byte> data)
    {
        var headerMap = new Dictionary<string, string>();
        var offset = 0;

        while (offset < data.Length)
        {
            var nameLength = data[offset++];
            var b = data.Slice(offset, nameLength);

#if NETSTANDARD
            var name = Encoding.UTF8.GetString(b.ToArray());
#else
            var name = Encoding.UTF8.GetString(b);
#endif
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

#if NETSTANDARD
            var value = Encoding.UTF8.GetString(b.ToArray());
#else
            var value = Encoding.UTF8.GetString(b);
#endif
            offset += headerValLength;
            headerMap.Add(name, value);
        }

        return headerMap;
    }
}