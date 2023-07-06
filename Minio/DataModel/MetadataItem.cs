using System;
using System.Collections.Generic;
using System.Text;

namespace Minio.DataModel
{
    [Serializable]
    public class MetadataItem
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public MetadataItem(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

    }
}
