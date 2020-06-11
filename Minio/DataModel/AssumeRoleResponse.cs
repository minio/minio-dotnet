using System;
using System.Xml.Serialization;

namespace Minio.DataModel
{
    [Serializable]
    [XmlRoot(ElementName = "AssumeRoleResponse", Namespace = "https://sts.amazonaws.com/doc/2011-06-15/")]
    public class AssumeRoleResponse
    {
        public AssumeRoleResult AssumeRoleResult { get; set; }
    }
}
