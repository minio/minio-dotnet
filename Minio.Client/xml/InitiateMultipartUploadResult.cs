using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Minio.Client.Xml
{
    [Serializable]
    [XmlRoot(ElementName = "InitiateMultipartUploadResult", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class InitiateMultipartUploadResult
    {
        public string UploadId { get; set; }
    }
}
