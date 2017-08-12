namespace Minio.Tests.Impl
{
    public class MinioClientImpl : DefaultMinioClient
    {
        public MinioClientImpl(MinioSettings minioSettings) : base(minioSettings)
        {
        }

        protected override SystemUserAgentSettings GetSystemUserAgentSettings()
        {
            return new SystemUserAgentSettings
            {
                ModelArch = "SIMULATOR",
                ModelDescription = "TEST AGENT",
                Platform = "Tests",
                AppVersion = "0.0"
            };
        }

        protected override string GetPlatformUserAgent()
        {
            throw new System.NotImplementedException();
        }
    }
}