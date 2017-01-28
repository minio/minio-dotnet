using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using testpolicy.MyPolicy;

namespace testpolicy
{
    public class Program
    {
        static void Main(string[] args)
        {
            String fileName = "C:\\Users\\vagrant\\go-net\\policy_jsonmod.txt";
            var stream = new MemoryStream(File.ReadAllBytes(fileName));
            var bucketName = "testminiopolicy";
            BucketPolicy policy = BucketPolicy.parseJson(stream, bucketName);
            Console.ReadLine();
        }
    }
}
