using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Minio.Client.xml
{
    [Serializable]
    public class Bucket
    {
        public string Name { get; set; }
        public string CreationDate { get; set; }
    }
}
