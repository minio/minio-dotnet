namespace Minio.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public class AbstractMinioTests
    {
        protected static Random random = new Random();

        protected const string TargetBasketName = "xamarin-tests";

        protected const string SpareBasketName = "xamarin-spare-tests";

        private const int RandomFileLength = 128;

        public AbstractMinioTests()
        {
            this.MinioClient = MinioFactory.CrerClient();
        }

        protected IMinioClient MinioClient { get; }


        protected Task<string> GetTargetBasketName()
        {
            return this.GetTBasketNameImpl(TargetBasketName);
        }


        protected Task<string> GetSpareBasketName()
        {
            return this.GetTBasketNameImpl(SpareBasketName);
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

        private async Task<string> GetTBasketNameImpl(string basketName)
        {
            var basketExists = await this.MinioClient.BucketExistsAsync(basketName);
            if (basketExists)
            {
                return basketName;
            }

            await this.MinioClient.MakeBucketAsync(basketName);

            return basketName;
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
            return await this.CreateFileForImpl(await this.GetTargetBasketName(), fileName, fileContent);
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
            return await this.CreateFileForImpl(await this.GetSpareBasketName(), fileName, fileContent);
        }

        protected async Task RemoveFileForTarget(string fileName)
        {
            await this.RemoveFileImpl(await this.GetTargetBasketName(), fileName);
        }

        protected async Task RemoveFileForSpare(string fileName)
        {
            await this.RemoveFileImpl(await this.GetSpareBasketName(), fileName);
        }

        private async Task<string> CreateFileForImpl(string basketName, string fileName, byte[] fileContent)
        {
            var stream = new MemoryStream(fileContent);
            await this.MinioClient.PutObjectAsync(basketName, fileName, stream);
            return fileName;
        }

        private async Task RemoveFileImpl(string basketName, string fileName)
        {
            try
            {
                await this.MinioClient.RemoveObjectAsync(basketName, fileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}