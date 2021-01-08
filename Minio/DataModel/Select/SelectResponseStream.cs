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
using Minio.Exceptions;

using System;
using System.Collections.Generic;

using System.IO;
using System.Xml.Serialization;
using System.Text;
using Force.Crc32;
using System.Linq;

namespace Minio.DataModel
{
    [Serializable]
    public class SelectResponseStream
    {
        // SelectResponseStream is a struct for selectobjectcontent response.
        
        public Stream Payload  { get ; set; }  

        [XmlElement("Stats", IsNullable = false)]
        public StatsMessage Stats { get; set; }

        [XmlElement("Progress", IsNullable = false)]
        public ProgressMessage Progress { get; set; }

        private byte[] prelude = new byte[8];
        private byte[] preludeCRC = new byte[4];
        private byte[] messageCRC = new byte[4];
        private byte[] headerValueLen = new byte[2];

        private MemoryStream payloadStream = null;
        private bool _isProcessing;
        
        public SelectResponseStream()
        {
        }

        protected int ReadFromStream(byte[] buffer)
        {
            int read = -1;
            if  (!this._isProcessing)
            {
               return read;
            }
            read = this.payloadStream.Read(buffer, 0, buffer.Length);
            if (!this.payloadStream.CanRead)
            {
                this._isProcessing = false;
            }
            return read;
        }
        public SelectResponseStream(Stream s)
        {
            if (s != null) {
                var _ms = new MemoryStream();
                s.CopyTo(_ms);
                this.payloadStream = _ms;
                this.Payload = new MemoryStream();
            }
            this._isProcessing = true;
            this.payloadStream.Seek(0, SeekOrigin.Begin);
            this.start();
        }
        
        private void start()
        {
            int numBytesRead = 0;
            while (_isProcessing)
            {   
                var n =  ReadFromStream(prelude);
                numBytesRead += n;
                n =  ReadFromStream(preludeCRC);
                var preludeCRCBytes = preludeCRC.ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(preludeCRCBytes); 
                }
                numBytesRead += n;
                var inputArray = new byte[prelude.Length + 4 ];
                System.Buffer.BlockCopy(prelude,0,inputArray,0, prelude.Length);

                // write real data to inputArray
                Crc32Algorithm.ComputeAndWriteToEnd(inputArray); // last 4 bytes contains CRC
                // transferring data or writing reading, and checking as final operation
                if (!Crc32Algorithm.IsValidWithCrcAtEnd(inputArray))
                {
                    throw new ArgumentException("invalid prelude CRC");
                }

                if (!Enumerable.SequenceEqual(inputArray.Skip(prelude.Length).Take(4), preludeCRCBytes))
                {
                    throw new ArgumentException("Prelude CRC Mismatch");
                }
                 var bytes = prelude.Take(4).ToArray();
                 if (BitConverter.IsLittleEndian)
                 {
                    Array.Reverse(bytes); 
                 }
                int totalLength = BitConverter.ToInt32(bytes, 0);
                bytes = prelude.Skip(4).Take(4).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes); 
                }

                int headerLength = BitConverter.ToInt32(bytes, 0);
                int payloadLength = totalLength - headerLength - 16;

                var headers = new byte[headerLength];
                var payload  = new byte[payloadLength];
                int num = ReadFromStream(headers);
                if (num != headerLength)
                {
                    throw new IOException("insufficient data");

                }
                num = ReadFromStream(payload);
                if (num != payloadLength)
                {
                    throw new IOException("insufficient data");
                }

                numBytesRead += num;
                num = ReadFromStream(messageCRC);
                var messageCRCBytes = messageCRC.ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(messageCRCBytes); 
                }
                // now verify message CRC
                inputArray = new byte[totalLength];
                System.Buffer.BlockCopy(prelude,0,inputArray,0, prelude.Length);
                System.Buffer.BlockCopy(preludeCRC,0,inputArray,prelude.Length, preludeCRC.Length);
                System.Buffer.BlockCopy(headers,0,inputArray,prelude.Length+ preludeCRC.Length, headerLength);
                System.Buffer.BlockCopy(payload,0,inputArray,prelude.Length+ preludeCRC.Length+headerLength, payloadLength);

                // write real data to inputArray
                Crc32Algorithm.ComputeAndWriteToEnd(inputArray); // last 4 bytes contains CRC
                // transferring data or writing reading, and checking as final operation
                if (!Crc32Algorithm.IsValidWithCrcAtEnd(inputArray))
                {
                    throw new ArgumentException("invalid message CRC");
                }

                if (!Enumerable.SequenceEqual(inputArray.Skip(totalLength-4).Take(4), messageCRCBytes))
                {
                    throw new ArgumentException("message CRC Mismatch");
                }
                Dictionary<String, String> headerMap = extractHeaders(headers);

                string value = null;
                if (headerMap.TryGetValue(":message-type", out value))
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
                        this._isProcessing = false;
                        break;
                    }
                    if (value.Equals("Cont") || payloadLength < 1)
                    {
                        continue;
                    }
                    if (value.Equals("Progress"))
                    {
                        ProgressMessage progress = new ProgressMessage();
                        using (var stream = new MemoryStream(payload))
                        progress = (ProgressMessage)new XmlSerializer(typeof(ProgressMessage)).Deserialize(stream);
                        this.Progress = progress;
                    }
                    if (value.Equals("Stats"))
                    {
                        Console.WriteLine("payload|"+Encoding.UTF7.GetString(payload));
                        StatsMessage stats = new StatsMessage();
                        using (var stream = new MemoryStream(payload))
                        stats = (StatsMessage)new XmlSerializer(typeof(StatsMessage)).Deserialize(stream);
                        this.Stats = stats;
                    }
                    if (value.Equals("Records"))
                    {
                        this.Payload.Write(payload,0, payloadLength);
                        continue;
                    }
                }
            }
            this._isProcessing = false;
            this.Payload.Seek(0, SeekOrigin.Begin);
            this.payloadStream.Close();
        }
        
        protected Dictionary<String,String> extractHeaders(byte[] data)
        {
            var headerMap = new Dictionary<String, String>();
            int offset = 0;
       
            while (offset < data.Length)
            {
                byte nameLength = data[offset++];
                byte[] b = data.Skip(offset).Take(nameLength).ToArray();
                String name = Encoding.UTF8.GetString(b, 0, b.Length);
                offset += nameLength;
                var hdrValue = data[offset++];
                if (hdrValue != 7)
                {
                    throw new IOException("header value type is not 7");
                }
                b = data.Skip(offset).Take(2).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b); 
                }
                offset += 2;
                int headerValLength = BitConverter.ToInt16(b, 0);
                b = data.Skip(offset).Take(headerValLength).ToArray();
                String value = Encoding.UTF8.GetString(b, 0, b.Length);
                offset += headerValLength;
                headerMap.Add(name,value);
            }
            return headerMap;
        }
    }
}
