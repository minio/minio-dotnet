using System;

namespace Minio.DataModel
{
    [Serializable]
    public class AssumeRoleResult
    {
        public AssumedRoleUser AssumedRoleUser { get; set; }
        public AssumeRoleResultCredentials Credentials { get; set; }
    }
}
