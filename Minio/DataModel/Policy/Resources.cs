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

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Minio.DataModel.Policy
{
    public class Resources : HashSet<string>
    {
        public Resources(string resource=null) : base()
        {
             if (resource != null)
            {
                Add(resource);
            }
        }
        public ISet<string> startsWith(string resourcePrefix)
        {
            HashSet<string> res = new HashSet<string>();
            foreach(string resource in this)
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
            string[] parts = Regex.Split(pattern, "\\*");
            if (parts.Length == 1)
            {
                return resource.Equals(parts[0]);
            }
            bool tglob = pattern.EndsWith("*");
            int end = parts.Length - 1;

            if (!resource.StartsWith(parts[0]))
            {
                return false;
            }
            for (int i = 1; i < end; i++)
            {
                if (!resource.Contains(parts[i]))
                {
                    return false;
                }
                int idx = resource.IndexOf(parts[i]) + parts[i].Length;
                resource = resource.Substring(idx);
            }
            return tglob || resource.EndsWith(parts[end]);

        }
        public Resources Match(string resource)
        {
            Resources res = new Resources();
            foreach (string pattern in this)
            {
                if (matched(pattern,resource))
                {
                    res.Add(pattern);
                }
            }
            return res;
        }
    }
}
