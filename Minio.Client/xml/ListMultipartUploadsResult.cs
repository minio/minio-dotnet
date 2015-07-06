using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Minio.Client.xml
{
    [Serializable]
    [XmlRoot(ElementName = "ListMultipartUploadsResult", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class ListMultipartUploadsResult
    {
        public string Bucket { get; set; }
        public string KeyMarker { get; set; }
        public string UploadIdMarker { get; set; }
        public string NextKeyMarker { get; set; }
        public string NextUploadIdMarker { get; set; }
        public int MaxUploads { get; set; }
        public bool IsTruncated { get; set; }
    }
}
