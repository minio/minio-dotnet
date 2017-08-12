namespace Minio
{
    using Extensions;
    using Foundation;
    using ObjCRuntime;
    using UIKit;

    internal class MinioClientImpl : DefaultMinioClient
    {
        public MinioClientImpl(MinioSettings minioSettings) : base(minioSettings)
        {
        }

        protected override SystemUserAgentSettings GetSystemUserAgentSettings()
        {
            return new SystemUserAgentSettings
            {
                ModelArch = Runtime.Arch.ToString(),
                ModelDescription = UIDevice.CurrentDevice.GetModelName(),
                Platform = "iOS",
                AppVersion = GetVersionName()
            };
        }

        protected override CryptoProvider CreateCryptoProvider()
        {
            return new CryptoProviderImpl();
        }

        protected override LogProvider CreateLogProvider()
        {
            return new LogProviderImpl();
        }

        protected override string GetPlatformUserAgent()
        {
            var webView = new UIWebView();
            return webView.EvaluateJavascript(@"navigator.userAgent");
        }

        private static string GetVersionName()
        {
            var ver = NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"];
            return ver.ToString();
        }
    }
}