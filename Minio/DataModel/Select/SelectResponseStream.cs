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

    public SelectResponseStream(Stream s)
    {
        if (s != null)
        {
            var _ms = new MemoryStream();
            s.CopyTo(_ms);
            payloadStream = _ms;
            Payload = new MemoryStream();
        }

        _isProcessing = true;
        payloadStream.Seek(0, SeekOrigin.Begin);
        start();
    }
    // SelectResponseStream is a struct for selectobjectcontent response.

    public Stream Payload { get; set; }

    [XmlElement("Stats", IsNullable = false)]
    public StatsMessage Stats { get; set; }

    [XmlElement("Progress", IsNullable = false)]
    public ProgressMessage Progress { get; set; }

    protected int ReadFromStream(Span<byte> buffer)
    {
        var read = -1;
        if (!_isProcessing) return read;
        read = payloadStream.Read(buffer);
        if (!payloadStream.CanRead) _isProcessing = false;
        return read;
    }

    private void start()
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
            var totalLength = BitConverter.ToInt32(bytes);
            bytes = prelude.Slice(4, 4).ToArray();
            if (BitConverter.IsLittleEndian) bytes.Reverse();

            var headerLength = BitConverter.ToInt32(bytes);
            var payloadLength = totalLength - headerLength - 16;

            Span<byte> headers = new byte[headerLength];
            Span<byte> payload = new byte[payloadLength];
            var num = ReadFromStream(headers);
            if (num != headerLength) throw new IOException("insufficient data");
            num = ReadFromStream(payload);
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
            payload.CopyTo(inputArray.Slice(prelude.Length + preludeCRC.Length + headerLength, payloadLength));

            var destinationMessage = inputArray.Slice(inputArray.Length - 4, 4);
            var isValidMessage = Crc32.TryHash(inputArray.Slice(0, inputArray.Length - 4), destinationMessage, out _);
            if (!isValidMessage) throw new ArgumentException("invalid message CRC");

            if (!destinationMessage.SequenceEqual(messageCRCBytes))
                throw new ArgumentException("message CRC Mismatch");

            var headerMap = extractHeaders(headers);

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
                    using (var stream = new MemoryStream(payload.ToArray()))
                    {
                        progress = (ProgressMessage)new XmlSerializer(typeof(ProgressMessage)).Deserialize(stream);
                    }

                    Progress = progress;
                }

                if (value.Equals("Stats"))
                {
                    var stats = new StatsMessage();
                    using (var stream = new MemoryStream(payload.ToArray()))
                    {
                        stats = (StatsMessage)new XmlSerializer(typeof(StatsMessage)).Deserialize(stream);
                    }

                    Stats = stats;
                }

                if (value.Equals("Records")) Payload.Write(payload);
            }
        }

        _isProcessing = false;
        Payload.Seek(0, SeekOrigin.Begin);
        payloadStream.Close();
    }

    protected Dictionary<string, string> extractHeaders(Span<byte> data)
    {
        var headerMap = new Dictionary<string, string>();
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
            int headerValLength = BitConverter.ToInt16(b);
            b = data.Slice(offset, headerValLength);
            var value = Encoding.UTF8.GetString(b);
            offset += headerValLength;
            headerMap.Add(name, value);
        }

        return headerMap;
    }
}