namespace Minio
{
    using System.Security.Cryptography;

    internal class CryptoProviderImpl : CryptoProvider
    {
        public override MD5 Md5 { get; } = new MD5Impl();
        public override SHA256 Sha256 { get; } = new SHA256Impl();
        public override HMACSHA Hmacsha { get; } = new HMACSHAImpl();

        public class MD5Impl : MD5
        {
            public override byte[] ComputeHash(byte[] content)
            {
                var md5 = System.Security.Cryptography.MD5.Create();
                return md5.ComputeHash(content);
            }
        }

        public class SHA256Impl : SHA256
        {
            public override byte[] ComputeHash(byte[] content)
            {
                var sha256 = System.Security.Cryptography.SHA256.Create();
                return sha256.ComputeHash(content);
            }
        }

        public class HMACSHAImpl : HMACSHA
        {
            public override byte[] ComputeHash(byte[] key, byte[] content)
            {
                var hmac = new HMACSHA256(key);
                hmac.Initialize();
                return hmac.ComputeHash(content);
            }
        }
    }
}