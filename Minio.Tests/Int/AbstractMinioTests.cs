namespace Minio.Tests.Int
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public class AbstractMinioTests
    {
        protected static Random random = new Random();

        protected const string TargetBucketName = "xamarin-tests";

        protected const string SpareBucketName = "xamarin-spare-tests";

        private const int RandomFileLength = 128;

        public AbstractMinioTests()
        {
            this.MinioClient = MinioFactory.CrerClient();
        }

        protected IMinioClient MinioClient { get; }


        protected Task<string> GetTargetBucketName()
        {
            return this.GetTBucketNameImpl(TargetBucketName);
        }


        protected Task<string> GetSpareBucketName()
        {
            return this.GetTBucketNameImpl(SpareBucketName);
        }

        protected string GetRandomName()
        {
            const string characters = "0123456789abcdefghijklmnopqrstuvwxyz";
            var result = new StringBuilder(5);
            for (var i = 0; i < 5; i++)
            {
                result.Append(characters[random.Next(characters.Length)]);
            }
            return "minio-xamarin-example-" + result;
        }

        private async Task<string> GetTBucketNameImpl(string bucketName)
        {
            var bucketExists = await this.MinioClient.BucketExistsAsync(bucketName);
            if (bucketExists)
            {
                return bucketName;
            }

            await this.MinioClient.MakeBucketAsync(bucketName);

            return bucketName;
        }

        protected byte[] GetRandomFile(int length = RandomFileLength)
        {
            var bytes = new byte[length];
            random.NextBytes(bytes);
            return bytes;
        }

        protected Task<string> CreateFileForTarget()
        {
            return this.CreateFileForTarget(this.GetRandomName());
        }

        protected async Task<string> CreateFileForTarget(string fileName)
        {
			return await this.CreateFileForTarget(fileName, this.GetRandomFile());
        }

        protected async Task<string> CreateFileForTarget(string fileName, byte[] fileContent)
        {
            return await this.CreateFileForImpl(await this.GetTargetBucketName(), fileName, fileContent);
        }

        protected Task<string> CreateFileForSpare()
        {
            return this.CreateFileForSpare(this.GetRandomName());
        }

        protected async Task<string> CreateFileForSpare(string fileName)
        {
			return await this.CreateFileForSpare(fileName, this.GetRandomFile());
        }

        protected async Task<string> CreateFileForSpare(string fileName, byte[] fileContent)
        {
            return await this.CreateFileForImpl(await this.GetSpareBucketName(), fileName, fileContent);
        }

        protected async Task RemoveFileForTarget(string fileName)
        {
            await this.RemoveFileImpl(await this.GetTargetBucketName(), fileName);
        }

        protected async Task RemoveFileForSpare(string fileName)
        {
            await this.RemoveFileImpl(await this.GetSpareBucketName(), fileName);
        }

        private async Task<string> CreateFileForImpl(string bucketName, string fileName, byte[] fileContent)
        {
            var stream = new MemoryStream(fileContent);
            await this.MinioClient.PutObjectAsync(bucketName, fileName, stream);
            return fileName;
        }

        private async Task RemoveFileImpl(string bucketName, string fileName)
        {
            try
            {
                await this.MinioClient.RemoveObjectAsync(bucketName, fileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}