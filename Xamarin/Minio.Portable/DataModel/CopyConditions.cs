/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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

namespace Minio.DataModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /**
    * A container class to hold all the Conditions to be checked
    * before copying an object.
    */
    public class CopyConditions
    {
        private readonly Dictionary<string, string> copyConditions = new Dictionary<string, string>();
        internal long ByteRangeEnd = -1;
        internal long ByteRangeStart = -1;

        /// <summary>
        ///     Clone CopyConditions object
        /// </summary>
        /// <returns>new CopyConditions object</returns>
        public CopyConditions Clone()
        {
            var newcond = new CopyConditions();
            foreach (var item in this.copyConditions)
            {
                newcond.copyConditions.Add(item.Key, item.Value);
            }
            newcond.ByteRangeStart = this.ByteRangeStart;
            newcond.ByteRangeEnd = this.ByteRangeEnd;
            return newcond;
        }


        /**
         * Set modified condition, copy object modified since given time.
         *
         * @throws ArgumentException
         *           When date is null
        */

        public void SetModified(DateTime date)
        {
            if (date == null)
            {
                throw new ArgumentException("Date cannot be empty");
            }
            this.copyConditions.Add("x-amz-copy-source-if-modified-since", date.ToUniversalTime().ToString("r"));
        }

        /// <summary>
        ///     Unset modified condition, copy object modified since given time.
        /// </summary>
        /// <param name="date"></param>
        /// <exception cref="ArgumentException"></exception>
        public void SetUnmodified(DateTime date)
        {
            if (date == null)
            {
                throw new ArgumentException("Date cannot be empty");
            }
            this.copyConditions.Add("x-amz-copy-source-if-unmodified-since", date.ToUniversalTime().ToString("r"));
        }

        /// <summary>
        ///     Set matching ETag condition, copy object which matches the following ETag.
        /// </summary>
        /// <param name="etag"></param>
        /// <exception cref="ArgumentException"></exception>
        public void SetMatchETag(string etag)
        {
            if (etag == null)
            {
                throw new ArgumentException("ETag cannot be empty");
            }
            this.copyConditions.Add("x-amz-copy-source-if-match", etag);
        }

        /// <summary>
        ///     Set matching ETag none condition, copy object which does not
        ///     match the following ETag.
        /// </summary>
        /// <param name="etag"></param>
        /// <exception cref="ArgumentException"></exception>
        public void SetMatchETagNone(string etag)
        {
            if (etag == null)
            {
                throw new ArgumentException("ETag cannot be empty");
            }
            this.copyConditions.Add("x-amz-copy-source-if-none-match", etag);
        }

        /// <summary>
        ///     Set Byte Range condition, copy object which falls within the
        ///     start and end byte range specified by user
        /// </summary>
        /// <param name="firstByte"></param>
        /// <param name="lastByte"></param>
        /// <exception cref="ArgumentException"></exception>
        public void SetByteRange(long firstByte, long lastByte)
        {
            if (firstByte < 0 || lastByte < firstByte)
            {
                throw new ArgumentException("Range start less than zero or range end less than range start");
            }

            this.ByteRangeStart = firstByte;
            this.ByteRangeEnd = lastByte;
        }

        /// <summary>
        ///     Get range size
        /// </summary>
        /// <returns></returns>
        public long GetByteRange()
        {
            return this.ByteRangeStart == -1 ? 0 : this.ByteRangeEnd - this.ByteRangeStart + 1;
        }

        /// <summary>
        ///     Get all the set copy conditions map.
        /// </summary>
        /// <returns></returns>
        public ReadOnlyDictionary<string, string> GetConditions()
        {
            return new ReadOnlyDictionary<string, string>(this.copyConditions);
        }
    }
}