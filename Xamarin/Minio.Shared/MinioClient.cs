namespace Minio
{
    public static class MinioClient
    {
        public static IMinioClient Create(string endpoint, string accessKey = "", string secretKey = "")
        {
            return Create(new MinioSettings(endpoint, accessKey, secretKey));
        }

        public static IMinioClient Create(MinioSettings settings)
        {
#if __ANDROID__
            return new MinioClientImpl(settings);
#elif __IOS__
			return new MinioClientImpl(settings);
#else
			throw new System.PlatformNotSupportedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
#endif
        }
    }
}