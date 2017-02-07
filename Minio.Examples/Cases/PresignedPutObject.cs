using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    public class PresignedPutObject
    {
        public static int Run()
        {
            /// Note: s3 AccessKey and SecretKey needs to be added in App.config file
            /// See instructions in README.md on running examples for more information.
            var client = new MinioClient(
                                 Environment.GetEnvironmentVariable("AWS_ENDPOINT"),
                                 Environment.GetEnvironmentVariable("AWS_ACCESS_KEY"),
                                 Environment.GetEnvironmentVariable("AWS_SECRET_KEY")
                                 ).WithSSL();

            Console.Out.WriteLine(client.Api.PresignedPutObject("my-bucketname", "my-objectname", 1000));
            return 0;
        }
    }
}
