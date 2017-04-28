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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Minio.DataModel
{
    /**
    * A container class to hold all the Conditions to be checked
    * before copying an object.
    */
    public class CopyConditions
    {
        private Dictionary<string, string> copyConditions = new Dictionary<string, string>();
        internal long byteRangeStart = -1;
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
            copyConditions.Add("x-amz-copy-source-if-modified-since", date.ToUniversalTime().ToString("r"));
        }

        /**
        * Unset modified condition, copy object modified since given time.
        *
        * @throws ArgumentException
        *           When date is null
       */

        public void SetUnmodified(DateTime date)
        {
            if (date == null)
            {
                throw new ArgumentException("Date cannot be empty");
            }
            copyConditions.Add("x-amz-copy-source-if-unmodified-since", date.ToUniversalTime().ToString("r"));
        }
        /**
         * Set matching ETag condition, copy object which matches
         * the following ETag.
         *
         * @throws ArgumentException when etag is null
         */
        public void SetMatchETag(string etag)
        {
            if (etag == null)
            {
                throw new ArgumentException("ETag cannot be empty");
            }
            copyConditions.Add("x-amz-copy-source-if-match", etag);
        }

        /**
         * Set matching ETag none condition, copy object which does not
         * match the following ETag.
         *
         * @throws InvalidArgumentException
         *           When etag is null
         */
        public void SetMatchETagNone(string etag)
        {
            if (etag == null)
            {
                throw new ArgumentException("ETag cannot be empty");
            }
            copyConditions.Add("x-amz-copy-source-if-none-match", etag);
        }

        /**
       * Set Byte Range condition, copy object which falls within the 
       * start and end byte range specified by user
       *
       * @throws InvalidArgumentException
       *           When firstByte is null or lastByte is null
       */
        public void SetByteRange(long firstByte, long lastByte)
        {
            if ((firstByte < 0) || (lastByte < firstByte))
                throw new ArgumentException("Range start less than zero or range end less than range start");

            this.byteRangeStart = firstByte;
            this.byteRangeEnd = lastByte;
        }
        /**
         * Get range size
         */
        public long GetByteRange()
        {
            return (this.byteRangeStart == -1) ? 0 : (this.byteRangeEnd - this.byteRangeStart + 1);
        }

        /**
         * Get all the set copy conditions map.
         *
         */
        public ReadOnlyDictionary<string, string> GetConditions()
        {
            return new ReadOnlyDictionary<string, string>(copyConditions);

        }

    }
}
