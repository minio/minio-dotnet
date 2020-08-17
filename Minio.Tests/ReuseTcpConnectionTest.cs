using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.Exceptions;

namespace Minio.Tests
{
    [TestClass]
    public class ReuseTcpConnectionTest
    {
        private MinioClient MinioClient { get; }
        public ReuseTcpConnectionTest()
        {
            this.MinioClient = new MinioClient("play.min.io", "Q3AM3UQ867SPQQA43P2F", "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG");
        }

        private async Task<bool> ObjectExistsAsync(MinioClient client, string bucket, string objectName)
        {
            try
            {
                await client.GetObjectAsync("bucket", objectName, stream => { });

                return true;
            }
            catch (ObjectNotFoundException e)
            {
                return false;
            }
        }

        [TestMethod]
        public async Task ReuseTcpTest()
        {
            var bucket = "bucket";
            var objectName = "object-name";

            if (!await this.MinioClient.BucketExistsAsync(bucket))
            {
                await this.MinioClient.MakeBucketAsync(bucket);
            }

            if (!await this.ObjectExistsAsync(this.MinioClient, bucket, objectName))
            {
                var helloData = Encoding.UTF8.GetBytes("hello world");
                var helloStream = new MemoryStream();
                helloStream.Write(helloData);
                helloStream.Seek(0, SeekOrigin.Begin);
                await this.MinioClient.PutObjectAsync(bucket, objectName, helloStream, helloData.Length);
            }

            var length = await this.GetObjectLength(bucket, objectName);

            Assert.IsTrue(length > 0);

            for (int i = 0; i < 100; i++)
            {
                // sequential execution, produce one tcp connection, check by netstat -an | grep 9000
                length = this.GetObjectLength(bucket, objectName).Result;
                Assert.IsTrue(length > 0);
            }

            Parallel.ForEach(Enumerable.Range(0, 500),
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = 8
                },
                i =>
                {
                    // concurrent execution, produce eight tcp connections.
                    length = this.GetObjectLength(bucket, objectName).Result;
                    Assert.IsTrue(length > 0);
                });
        }

        private async Task<double> GetObjectLength(string bucket, string objectName)
        {
            long objectLength = 0;
            await this.MinioClient.GetObjectAsync(bucket, objectName, stream => { objectLength = stream.Length; });

            return objectLength;
        }
    }
}