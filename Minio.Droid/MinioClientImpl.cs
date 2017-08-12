namespace Minio
{
    using Android.App;
    using Android.Webkit;
    using Minio.Extensions;

    internal class MinioClientImpl : DefaultMinioClient
    {
        public MinioClientImpl(MinioSettings minioSettings) : base(minioSettings)
        {
        }

        protected override SystemUserAgentSettings GetSystemUserAgentSettings()
        {
            return new SystemUserAgentSettings
            {
                ModelArch = DeviceExtension.GetArchType(),
                ModelDescription = Android.OS.Build.Model,
                Platform = "Android",
                AppVersion = GetVersionName()
            };
        }

        protected override string GetPlatformUserAgent()
        {
            var webView = new WebView(Application.Context);
            var settings = webView.Settings;
            return settings.UserAgentString;
        }

        protected override LogProvider CreateLogProvider()
        {
            return new LogProviderImpl();
        }

        protected override CryptoProvider CreateCryptoProvider()
        {
            return new CryptoProviderImpl();
        }

        private static string GetVersionName()
        {
            var context = Application.Context;
            return context.PackageManager.GetPackageInfo(context.PackageName, 0).VersionName;
        }
    }
}