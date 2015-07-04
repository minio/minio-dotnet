using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Minio.Client.xml
{
    [Serializable]
    [XmlRoot(ElementName = "ListAllMyBucketsResult", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    [XmlInclude(typeof(Bucket))]
    public class ListAllMyBucketsResult
    {
        [XmlAttribute]
        public string Owner { get; set; }
        [XmlArray("Buckets")]
        [XmlArrayItem(typeof(Bucket))]
        public List<Bucket> Buckets { get; set; }
    }
}
