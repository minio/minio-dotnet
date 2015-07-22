using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Minio.Client.Xml
{
    [Serializable]
    [XmlRoot(ElementName = "ListBucketResult", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    [XmlInclude(typeof(Item))]
    [XmlInclude(typeof(Prefix))]
    public class ListBucketResult
    {
        public string Name { get; set; }

        public string Prefix { get; set; }

        public string Marker { get; set; }

        public string NextMarker { get; set; }

        public string MaxKeys { get; set; }

        public string Delimiter { get; set; }

        public bool IsTruncated { get; set; }
    }
}
