using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Minio.Client.Xml
{
    [Serializable]
    [XmlRoot(ElementName = "ListPartsResult", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class ListPartsResult
    {
        public int NextPartNumberMarker { get; set; }
        public bool IsTruncated { get; set; }
    }
}
