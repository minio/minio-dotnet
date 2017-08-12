namespace Minio
{
#if NETSTANDARD1_6 || NET452
    using System.Security.Cryptography;
#else
    using System;
#endif

    public class CryptoProvider
    {
        public virtual MD5 Md5 { get; } = new MD5();

        public virtual SHA256 Sha256 { get; } = new SHA256();

        public virtual HMACSHA Hmacsha { get; } = new HMACSHA();

        public class MD5
        {
            public virtual byte[] ComputeHash(byte[] content)
            {
#if NETSTANDARD1_6 || NET452
                var md5 = System.Security.Cryptography.MD5.Create();
                return md5.ComputeHash(content);
#else
                throw new PlatformNotSupportedException();
#endif
            }
        }

        public class SHA256
        {
            public virtual byte[] ComputeHash(byte[] content)
            {
#if NETSTANDARD1_6 || NET452
                var sha256 = System.Security.Cryptography.SHA256.Create();
                return sha256.ComputeHash(content);
#else
                throw new PlatformNotSupportedException();
#endif
            }
        }

        public class HMACSHA
        {
            public virtual byte[] ComputeHash(byte[] key, byte[] content)
            {
#if NETSTANDARD1_6 || NET452
                var hmac = new HMACSHA256(key);
                hmac.Initialize();
                return hmac.ComputeHash(content);
#else
                throw new PlatformNotSupportedException();
#endif
            }
        }
    }
}