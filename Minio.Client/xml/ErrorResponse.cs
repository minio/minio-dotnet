using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Minio.Client
{
    [Serializable]
    [XmlRoot(ElementName="Error", Namespace = "")]
     public class ErrorResponse
     {
        [XmlAttribute]
         public string Code { get; set; }
        [XmlAttribute]
         public string Message { get; set; }
        [XmlAttribute]
         public string RequestID { get; set; }
        [XmlAttribute]
         public string HostID { get; set; }
        [XmlAttribute]
         public string Resource { get; set; }
        
        // not an attribute, we fix it up later
         public string XAmzID2 { get; set; }
    }
}
