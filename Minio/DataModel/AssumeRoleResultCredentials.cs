using System;

namespace Minio.DataModel
{
    [Serializable]
    public class AssumeRoleResultCredentials
    {
        public string AccessKeyId { get; set; }
        public string SecretAccessKey { get; set; }
        public string SessionToken { get; set; }
        public DateTime Expiration { get; set; }
    }
}
