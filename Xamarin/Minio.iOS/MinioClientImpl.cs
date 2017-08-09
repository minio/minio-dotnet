﻿﻿namespace Minio
{
    using Foundation;
    using Minio.Extensions;
    using ObjCRuntime;
    using UIKit;

    internal class MinioClientImpl : AbstractMinioClient
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

        protected override string GetPlatformUserAgent()
        {
            var webView = new UIWebView();
            return webView.EvaluateJavascript(@"navigator.userAgent");
        }

        protected override CryptoProvider CreateCryptoProvider()
        {
            return new CryptoProviderImpl();
        }

        protected override LogProvider CreateLogProvider()
        {
            return new LogProviderImpl();
        }

        private static string GetVersionName()
        {
            var ver = NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"];
            return ver.ToString();
        }
    }
}