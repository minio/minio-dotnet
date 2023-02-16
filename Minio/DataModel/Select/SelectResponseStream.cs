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

using System.Text;
using System.Xml.Serialization;
using Force.Crc32;
using Minio.Exceptions;

namespace Minio.DataModel;

[Serializable]
public class SelectResponseStream
{
    private bool _isProcessing;
    private byte[] headerValueLen = new byte[2];
    private byte[] messageCRC = new byte[4];

    private MemoryStream payloadStream;

    private byte[] prelude = new byte[8];
    private byte[] preludeCRC = new byte[4];

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

    protected int ReadFromStream(byte[] buffer)
    {
        var read = -1;
        if (!_isProcessing) return read;
        read = payloadStream.Read(buffer, 0, buffer.Length);
        if (!payloadStream.CanRead) _isProcessing = false;
        return read;
    }

    private void start()
    {
        var numBytesRead = 0;
        while (_isProcessing)
        {
            var n = ReadFromStream(prelude);
            numBytesRead += n;
            n = ReadFromStream(preludeCRC);
            var preludeCRCBytes = preludeCRC.ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(preludeCRCBytes);
            numBytesRead += n;
            var inputArray = new byte[prelude.Length + 4];
            Buffer.BlockCopy(prelude, 0, inputArray, 0, prelude.Length);

            // write real data to inputArray
            Crc32Algorithm.ComputeAndWriteToEnd(inputArray); // last 4 bytes contains CRC
            // transferring data or writing reading, and checking as final operation
            if (!Crc32Algorithm.IsValidWithCrcAtEnd(inputArray)) throw new ArgumentException("invalid prelude CRC");

            if (!inputArray.Skip(prelude.Length).Take(4).SequenceEqual(preludeCRCBytes))
                throw new ArgumentException("Prelude CRC Mismatch");
            var bytes = prelude.Take(4).ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            var totalLength = BitConverter.ToInt32(bytes, 0);
            bytes = prelude.Skip(4).Take(4).ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);

            var headerLength = BitConverter.ToInt32(bytes, 0);
            var payloadLength = totalLength - headerLength - 16;

            var headers = new byte[headerLength];
            var payload = new byte[payloadLength];
            var num = ReadFromStream(headers);
            if (num != headerLength) throw new IOException("insufficient data");
            num = ReadFromStream(payload);
            if (num != payloadLength) throw new IOException("insufficient data");

            numBytesRead += num;
            num = ReadFromStream(messageCRC);
            var messageCRCBytes = messageCRC.ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(messageCRCBytes);
            // now verify message CRC
            inputArray = new byte[totalLength];
            Buffer.BlockCopy(prelude, 0, inputArray, 0, prelude.Length);
            Buffer.BlockCopy(preludeCRC, 0, inputArray, prelude.Length, preludeCRC.Length);
            Buffer.BlockCopy(headers, 0, inputArray, prelude.Length + preludeCRC.Length, headerLength);
            Buffer.BlockCopy(payload, 0, inputArray, prelude.Length + preludeCRC.Length + headerLength, payloadLength);

            // write real data to inputArray
            Crc32Algorithm.ComputeAndWriteToEnd(inputArray); // last 4 bytes contains CRC
            // transferring data or writing reading, and checking as final operation
            if (!Crc32Algorithm.IsValidWithCrcAtEnd(inputArray)) throw new ArgumentException("invalid message CRC");

            if (!inputArray.Skip(totalLength - 4).Take(4).SequenceEqual(messageCRCBytes))
                throw new ArgumentException("message CRC Mismatch");
            var headerMap = extractHeaders(headers);

            if (headerMap.TryGetValue(":message-type", out string value))
            {
                if (value.Equals(":error"))
                {
                    string errorCode = null;
                    string errorMessage = null;
                    headerMap.TryGetValue(":error-code", out errorCode);
                    headerMap.TryGetValue(":error-message", out errorMessage);
                    throw new SelectObjectContentException(errorCode + ":" + errorMessage);
                }
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
                    using (var stream = new MemoryStream(payload))
                    {
                        progress = (ProgressMessage)new XmlSerializer(typeof(ProgressMessage)).Deserialize(stream);
                    }

                    Progress = progress;
                }

                if (value.Equals("Stats"))
                {
                    var stats = new StatsMessage();
                    using (var stream = new MemoryStream(payload))
                    {
                        stats = (StatsMessage)new XmlSerializer(typeof(StatsMessage)).Deserialize(stream);
                    }

                    Stats = stats;
                }

                if (value.Equals("Records")) Payload.Write(payload, 0, payloadLength);
            }
        }

        _isProcessing = false;
        Payload.Seek(0, SeekOrigin.Begin);
        payloadStream.Close();
    }

    protected Dictionary<string, string> extractHeaders(byte[] data)
    {
        var headerMap = new Dictionary<string, string>();
        var offset = 0;

        while (offset < data.Length)
        {
            var nameLength = data[offset++];
            var b = data.Skip(offset).Take(nameLength).ToArray();
            var name = Encoding.UTF8.GetString(b, 0, b.Length);
            offset += nameLength;
            var hdrValue = data[offset++];
            if (hdrValue != 7) throw new IOException("header value type is not 7");
            b = data.Skip(offset).Take(2).ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            offset += 2;
            int headerValLength = BitConverter.ToInt16(b, 0);
            b = data.Skip(offset).Take(headerValLength).ToArray();
            var value = Encoding.UTF8.GetString(b, 0, b.Length);
            offset += headerValLength;
            headerMap.Add(name, value);
        }

        return headerMap;
    }
}