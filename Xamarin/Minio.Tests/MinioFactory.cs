namespace Minio.Tests
{
    using Minio.Tests.Impl;

    public static class MinioFactory
    {
        public static IMinioClient CrerClient()
        {
            var minioClient =  new MinioClientImpl(new MinioSettings(
                Constants.Minio.Endpoint,
                Constants.Minio.AccessKey,
                Constants.Minio.SecretKey))
                .WithSsl();

			// minioClient.Trace = true;

			return minioClient;
        }
    }
}