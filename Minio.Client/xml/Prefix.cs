using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Minio.Client.Xml
{
    [Serializable]
    public class Prefix
    {
        [XmlAttribute("Prefix")]
        public string Name { get; set; }
    }
}
