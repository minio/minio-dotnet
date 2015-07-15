using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Client
{
    class Regions
    {
        private Regions()
        {

        }

        internal static string GetRegion(string host)
        {
            switch (host)
            {
                case "s3-ap-northeast-1.amazonaws.com": return "ap-northeast-1";
                case "s3-ap-southeast-1.amazonaws.com": return "ap-southeast-1";
                case "s3-ap-southeast-2.amazonaws.com": return "ap-southeast-2";
                case "s3-eu-central-1.amazonaws.com": return "eu-central-1";
                case "s3-eu-west-1.amazonaws.com": return "eu-west-1";
                case "s3-sa-east-1.amazonaws.com": return "sa-east-1";
                case "s3.amazonaws.com": return "us-east-1";
                case "s3-external-1.amazonaws.com": return "us-east-1";
                case "s3-us-west-1.amazonaws.com": return "us-west-1";
                case "s3-us-west-2.amazonaws.com": return "us-west-2";
                default: return "milkyway";
            }
        }
    }
}

