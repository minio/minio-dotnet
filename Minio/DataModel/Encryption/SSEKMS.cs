/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2019 MinIO, Inc.
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
using Minio.Helper;

namespace Minio.DataModel.Encryption
{
    /// <summary>
    ///     Server-side encryption with AWS KMS managed keys
    /// </summary>
    public class SSEKMS : IServerSideEncryption
    {
        public SSEKMS(string key, IDictionary<string, string> context = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("KMS Key cannot be empty", nameof(key));
            }

            Key = key;
            Context = context;
        }

        protected IDictionary<string, string> Context { get; set; }

        // Specifies the customer master key(CMK).Cannot be null
        protected string Key { get; set; }

        public EncryptionType GetEncryptionType()
        {
            return EncryptionType.SSE_KMS;
        }

        public void Marshal(IDictionary<string, string> headers)
        {
            if (headers is null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            headers.Add(Constants.SSEKMSKeyId, Key);
            headers.Add(Constants.SSEGenericHeader, "aws:kms");
            if (Context is not null)
            {
                headers.Add(Constants.SSEKMSContext, MarshalContext());
            }
        }

        /// <summary>
        ///     Serialize context into JSON string.
        /// </summary>
        /// <returns>Serialized JSON context</returns>
        private string MarshalContext()
        {
            var sb = new StringBuilder();

            sb.Append('{');
            var i = 0;
            var len = Context.Count;
            foreach (var pair in Context)
            {
                sb.Append('"').Append(pair.Key).Append('"');
                sb.Append(':');
                sb.Append('"').Append(pair.Value).Append('"');
                i++;
                if (i != len)
                {
                    sb.Append(':');
                }
            }

            sb.Append('}');
            ReadOnlySpan<byte> contextBytes = Encoding.UTF8.GetBytes(sb.ToString());
#if NETSTANDARD
            return Convert.ToBase64String(contextBytes.ToArray());
#else
            return Convert.ToBase64String(contextBytes);
#endif
        }
    }
}