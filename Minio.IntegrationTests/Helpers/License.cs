public static class License
{
    public static string Minio => Environment.GetEnvironmentVariable("MINIO_LICENSE") ?? throw new Exception("MINIO_LICENSE environment variable is not set");
}