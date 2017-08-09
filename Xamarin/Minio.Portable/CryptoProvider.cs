namespace Minio
{
    public abstract class CryptoProvider
    {
        public abstract MD5 Md5 { get; }

        public abstract SHA256 Sha256 { get; }

        public abstract HMACSHA Hmacsha { get; }

        public abstract class MD5
        {
            public abstract byte[] ComputeHash(byte[] content);
        }

        public abstract class SHA256
        {
            public abstract byte[] ComputeHash(byte[] content);
        }

        public abstract class HMACSHA
        {
            public abstract byte[] ComputeHash(byte[] key, byte[] content);
        }
    }
}