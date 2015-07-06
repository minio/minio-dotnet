using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Client.xml
{
    [Serializable]
    public class Part
    {
        public int PartNumber { get; set; }

        public string ETag { get; set; }
    }
}
