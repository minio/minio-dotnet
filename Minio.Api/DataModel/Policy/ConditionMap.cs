using System.Collections.Generic;
namespace Minio.DataModel
{
    internal class ConditionMap: Dictionary<string,ConditionKeyMap>
    {
        public ConditionMap(string key=null,ConditionKeyMap value=null): base()
        {
            if (key != null && value != null)
            {
                this.Add(key, value);
            }
        }
      
        public ConditionKeyMap put(string key,ConditionKeyMap value)
        {
            ConditionKeyMap existingValue;
            base.TryGetValue(key,out existingValue);
            if (existingValue == null)
            {
                existingValue = new ConditionKeyMap(value);
            } 
            else
            {
                foreach (var item in value)
                {
                    existingValue.Add(item.Key, item.Value);
                }
            }
            this[key] = existingValue;
            return existingValue;
        }
        public void putAll(ConditionMap cmap)
        {
            foreach (var item in cmap)
            {
                this[item.Key] = item.Value;
            }
        }

    }
}
