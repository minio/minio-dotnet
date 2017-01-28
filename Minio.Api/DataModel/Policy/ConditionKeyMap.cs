using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.DataModel
{
    internal class ConditionKeyMap:Dictionary<string,ISet<string>>
    {
        public ConditionKeyMap(ConditionKeyMap map=null):base(map)
        {
        }
        public ConditionKeyMap(string key, ISet<string> value)
        {
            this.Add(key, value);
        }
        public ISet<string> put(string key, string value)
        {
            ISet<string> set = new HashSet<string>();
            set.Add(value);
            this.Add(key, set);
            return set;
        }
        public ISet<string> put(string key, ISet<string> value)
        {
            ISet<string> existingValue;
            this.TryGetValue(key, out existingValue);
            if (existingValue == null)
            {
                existingValue = new HashSet<string>();
            }
            existingValue.UnionWith(value);
            this[key] = existingValue;
            return existingValue;
        }
        public void remove(string key,ISet<string> value)
        {
            ISet<string> existingValue;
            this.TryGetValue(key, out existingValue);
            if (existingValue == null)
            {
                return;
            }
            existingValue.Except(value);
            if (existingValue.Count == 0)
            {
                this.Remove(key);
            }
            this[key] = existingValue;
        }

    }
}
