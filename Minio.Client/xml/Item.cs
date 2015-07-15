using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Client.xml
{
    [Serializable]
    public class Item
    {
        public string Key { get; set; }
        public string LastModified { get; set; }
        public string ETag { get; set; }
        public UInt64 Size { get; set; }

        public bool IsDir { get; set; }

        public DateTime LastModifiedDateTime
        {
            get
            {
                return DateTime.Parse(this.LastModified);
            }
        }
    }
}
