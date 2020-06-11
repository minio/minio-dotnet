using System;

namespace Minio.DataModel
{
    [Serializable]
    public class AssumedRoleUser
    {
        public string Arn { get; set; }
        public string AssumeRoleId { get; set; }
    }
}
