namespace Minio
{
    using System.Security.Cryptography;

    internal class CryptoProviderImpl : CryptoProvider
    {
        public CryptoProviderImpl()
        {
            this.Md5 = new Md5Impl();
            this.Sha256 = new SHA256Impl();
            this.Hmacsha = new HMACSHAImpl();
        }

        public override MD5 Md5 { get; }

        public override SHA256 Sha256 { get; }

        public override HMACSHA Hmacsha { get; }

        private class Md5Impl : MD5
        {
            public override byte[] ComputeHash(byte[] content)
            {
                var md5 = System.Security.Cryptography.MD5.Create();
                return md5.ComputeHash(content);
            }
        }

        private class SHA256Impl : SHA256
        {
            public override byte[] ComputeHash(byte[] content)
            {
                var sha256 = System.Security.Cryptography.SHA256.Create();
                return sha256.ComputeHash(content);
            }
        }

        private class HMACSHAImpl : HMACSHA
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