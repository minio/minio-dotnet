using System;
using System.Collections.Concurrent;

namespace Minio
{

    /**
     * Amazon AWS S3 endpoints for various regions.
     */
    public sealed class AWSS3Endpoints
    {
        private static readonly Lazy<AWSS3Endpoints> lazy =
            new Lazy<AWSS3Endpoints>(() => new AWSS3Endpoints());

        private ConcurrentDictionary<string, string> endpoints;

        public static AWSS3Endpoints Instance
        {
            get { return lazy.Value; }
        }
        private AWSS3Endpoints()
        {
            endpoints = new ConcurrentDictionary<string, string>();
            // ap-northeast-1
            endpoints.TryAdd("ap-northeast-1", "s3-ap-northeast-1.amazonaws.com");
            // ap-northeast-2
            endpoints.TryAdd("ap-northeast-2", "s3-ap-northeast-2.amazonaws.com");
            //ap-south-1
            endpoints.TryAdd("ap-south-1", "s3-ap-south-1.amazonaws.com");
            // ap-southeast-1
            endpoints.TryAdd("ap-southeast-1", "s3-ap-southeast-1.amazonaws.com");
            // ap-southeast-2
            endpoints.TryAdd("ap-southeast-2", "s3-ap-southeast-2.amazonaws.com");
            // eu-central-1
            endpoints.TryAdd("eu-central-1", "s3-eu-central-1.amazonaws.com");
            // eu-west-1
            endpoints.TryAdd("eu-west-1", "s3-eu-west-1.amazonaws.com");
            // eu-west-2
            endpoints.TryAdd("eu-west-2", "s3-eu-west-2.amazonaws.com");
            // sa-east-1
            endpoints.TryAdd("sa-east-1", "s3-sa-east-1.amazonaws.com");
            // us-west-1
            endpoints.TryAdd("us-west-1", "s3-us-west-1.amazonaws.com");
            // us-west-2
            endpoints.TryAdd("us-west-2", "s3-us-west-2.amazonaws.com");
            // us-east-1
            endpoints.TryAdd("us-east-1", "s3.amazonaws.com");
            // us-east-2
            endpoints.TryAdd("us-east-2", "s3-us-east-2.amazonaws.com");
            //ca-central-1
            endpoints.TryAdd("ca-central-1", "s3.ca-central-1.amazonaws.com");
            // cn-north-1
            endpoints.TryAdd("cn-north-1", "s3.cn-north-1.amazonaws.com.cn");
        }

        /**
         * Gets Amazon S3 endpoint for the relevant region.
         */
        public string endpoint(string region)
        {

            string endpoint = null;
            if (region != null)
            {
                AWSS3Endpoints.Instance.endpoints.TryGetValue(region, out endpoint);
            }
            if (endpoint == null)
            {
                endpoint = "s3.amazonaws.com";
            }
            return endpoint;
        }

    }
}

