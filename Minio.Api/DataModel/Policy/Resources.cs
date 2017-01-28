using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
namespace Minio.DataModel
{
    internal class Resources : HashSet<string>
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
        public ISet<string> match(string resource)
        {
            ISet<string> res = new HashSet<string>();
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
