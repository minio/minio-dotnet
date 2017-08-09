namespace Minio.Tests.Impl
{
    public class MinioClientImpl : AbstractMinioClient
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

        protected override CryptoProvider CreateCryptoProvider()
        {
            return new CryptoProviderImpl();
        }

        protected override LogProvider CreateLogProvider()
        {
            return new LogProviderImpl();
        }
    }
}