using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.DataModel
{
 /**
 * A container class to hold all the Conditions to be checked
 * before copying an object.
 */
    public class CopyConditions
    {
        private Dictionary<string, string> copyConditions;

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
            copyConditions["x-amz-copy-source-if-modified-since"] = date.ToUniversalTime().ToString("r");
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
            copyConditions["x-amz-copy-source-if-unmodified-since"] = date.ToUniversalTime().ToString("r");
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
            copyConditions["x-amz-copy-source-if-match"] =  etag;
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
            copyConditions["x-amz-copy-source-if-none-match"] =  etag;
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
