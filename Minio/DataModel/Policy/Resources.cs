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

namespace Minio.DataModel.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class Resources : HashSet<string>
    {
        public Resources(string resource = null)
        {
            if (resource != null)
            {
                this.Add(resource);
            }
        }

        public ISet<string> StartsWith(string resourcePrefix)
        {
            var res = new HashSet<string>();
            foreach (var resource in this)
            {
                if (resource.StartsWith(resourcePrefix))
                {
                    res.Add(resource);
                }
            }
            return res;
        }

        private bool matched(string pattern, string resource)
        {
            if (pattern.Length == 0)
            {
                return resource.Equals(pattern);
            }
            if (pattern.Equals("*"))
            {
                return true;
            }
            var parts = Regex.Split(pattern, "\\*");
            if (parts.Length == 1)
            {
                return resource.Equals(parts[0]);
            }
            var tglob = pattern.EndsWith("*");
            var end = parts.Length - 1;

            if (!resource.StartsWith(parts[0]))
            {
                return false;
            }
            for (var i = 1; i < end; i++)
            {
                if (!resource.Contains(parts[i]))
                {
                    return false;
                }
                var idx = resource.IndexOf(parts[i], StringComparison.Ordinal) + parts[i].Length;
                resource = resource.Substring(idx);
            }
            return tglob || resource.EndsWith(parts[end]);
        }

        internal Resources Match(string resource)
        {
            var res = new Resources();
            foreach (var pattern in this)
            {
                if (this.matched(pattern, resource))
                {
                    res.Add(pattern);
                }
            }
            return res;
        }
    }
}