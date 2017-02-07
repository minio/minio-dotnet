using Minio.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    public class PresignedPostPolicy
    {
        public static int Run()
        {
            /// Note: s3 AccessKey and SecretKey needs to be added in App.config file
            /// See instructions in README.md on running examples for more information.
            var client = new MinioRestClient(
                                 Environment.GetEnvironmentVariable("AWS_ENDPOINT"),
                                 Environment.GetEnvironmentVariable("AWS_ACCESS_KEY"),
                                 Environment.GetEnvironmentVariable("AWS_SECRET_KEY")
                                 ).WithSSL();

            PostPolicy form = new PostPolicy();
            DateTime expiration = DateTime.UtcNow;
            form.SetExpires(expiration.AddDays(10));
            form.SetKey("my-objectname");
            form.SetBucket("my-bucketname");

            Dictionary<string, string> formData = client.Api.PresignedPostPolicy(form);
            string curlCommand = "curl ";
            foreach (KeyValuePair<string, string> pair in formData)
            {
                curlCommand = curlCommand + " -F " + pair.Key + "=" + pair.Value;
            }
            curlCommand = curlCommand + " -F file=@/etc/bashrc https://s3.amazonaws.com/my-bucketname";
            Console.Out.WriteLine(curlCommand);
            return 0;
        }
    }
}
