/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Minio.DataModel
{
    /// <summary>
    /// A container class to hold all the Conditions to be checked before copying an object.
    /// </summary>
    public class CopyConditions
    {
        private readonly Dictionary<string, string> copyConditions = new Dictionary<string, string>();
        internal long byteRangeStart = 0;
        internal long byteRangeEnd = -1;

        /// <summary>
        /// Clone CopyConditions object
        /// </summary>
        /// <returns>new CopyConditions object</returns>
        public CopyConditions Clone()
        {
            CopyConditions newcond = new CopyConditions();
            foreach (KeyValuePair<string, string> item in this.copyConditions)
            {
                newcond.copyConditions.Add(item.Key, item.Value);
            }
            newcond.byteRangeStart = this.byteRangeStart;
            newcond.byteRangeEnd = this.byteRangeEnd;
            return newcond;
        }

        /// <summary>
        /// Set modified condition, copy object modified since given time.
        /// </summary>
        /// <param name="date"></param>
        /// <exception cref="ArgumentException">When date is null</exception>
        public void SetModified(DateTime date)
        {
            copyConditions.Add("x-amz-copy-source-if-modified-since", date.ToUniversalTime().ToString("r"));
        }

        /// <summary>
        /// Unset modified condition, copy object modified since given time.
        /// </summary>
        /// <param name="date"></param>
        /// <exception cref="ArgumentException">When date is null</exception>
        public void SetUnmodified(DateTime date)
        {
            copyConditions.Add("x-amz-copy-source-if-unmodified-since", date.ToUniversalTime().ToString("r"));
        }

        /// <summary>
        /// Set matching ETag condition, copy object which matches
        /// the following ETag.
        /// </summary>
        /// <param name="etag"></param>
        /// <exception cref="ArgumentException">When etag is null</exception>
        public void SetMatchETag(string etag)
        {
            if (etag == null)
            {
                throw new ArgumentException("ETag cannot be empty", nameof(etag));
            }
            copyConditions.Add("x-amz-copy-source-if-match", etag);
        }

        /// <summary>
        /// Set matching ETag none condition, copy object which does not
        /// match the following ETag.
        /// </summary>
        /// <param name="etag"></param>
        /// <exception cref="ArgumentException">When etag is null</exception>
        public void SetMatchETagNone(string etag)
        {
            if (etag == null)
            {
                throw new ArgumentException("ETag cannot be empty", nameof(etag));
            }
            copyConditions.Add("x-amz-copy-source-if-none-match", etag);
        }

        /// <summary>
        /// Set replace metadata directive which specifies that server side copy needs to replace metadata
        /// on destination with custom metadata provided in the request.
        /// </summary>
        public void SetReplaceMetadataDirective()
        {
            copyConditions.Add("x-amz-metadata-directive", "REPLACE");
        }

        /// <summary>
        /// Return true if replace metadata directive is specified
        /// </summary>
        /// <returns></returns>
        public bool HasReplaceMetadataDirective()
        {
            foreach (var item in copyConditions)
            {
                if (item.Key.Equals("x-amz-metadata-directive", StringComparison.OrdinalIgnoreCase) &&
                    item.Value.ToUpper().Equals("REPLACE"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set Byte Range condition, copy object which falls within the
        /// start and end byte range specified by user
        /// </summary>
        /// <param name="firstByte"></param>
        /// <param name="lastByte"></param>
        /// <exception cref="ArgumentException">When firstByte is null or lastByte is null</exception>
        public void SetByteRange(long firstByte, long lastByte)
        {
            if ((firstByte < 0) || (lastByte < firstByte))
            {
                throw new ArgumentException("Range start less than zero or range end less than range start");
            }

            this.byteRangeStart = firstByte;
            this.byteRangeEnd = lastByte;
        }

        /// <summary>
        /// Get range size
        /// </summary>
        /// <returns></returns>
        public long GetByteRange()
        {
            return (this.byteRangeStart == -1) ? 0 : (this.byteRangeEnd - this.byteRangeStart + 1);
        }

        /// <summary>
        /// Get all the set copy conditions map.
        /// </summary>
        /// <returns></returns>
        public ReadOnlyDictionary<string, string> GetConditions()
        {
            return new ReadOnlyDictionary<string, string>(copyConditions);
        }
    }
}
