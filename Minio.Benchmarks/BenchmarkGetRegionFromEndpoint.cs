/*
* MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
* (C) 2017, 2018, 2019, 2020 MinIO, Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using BenchmarkDotNet.Attributes;

namespace Minio.Benchmarks
{
    public class BenchmarkGetRegionFromEndpoint
    {
        private string awsEndpoint = "my-bucket.s3.us-west-2.amazonaws.com";
        private string nonAwsEndpoint = "minio.mydomain.com";
        private string nonAwsEndpointPort = "subdomain.minio.mydomain.com:9000";

        public BenchmarkGetRegionFromEndpoint()
        {
        }

        [Benchmark]
        public string OriginalAws() => OriginalRegions.GetRegionFromEndpoint(awsEndpoint);

        [Benchmark]
        public string OriginalNonAws() => OriginalRegions.GetRegionFromEndpoint(nonAwsEndpoint);

        [Benchmark]
        public string OriginalNonAwsPort() => OriginalRegions.GetRegionFromEndpoint(nonAwsEndpointPort);

        [Benchmark]
        public string PreCompiledAws() => PreCompiledRegions.GetRegionFromEndpoint(awsEndpoint);

        [Benchmark]
        public string PreCompiledNonAws() => PreCompiledRegions.GetRegionFromEndpoint(nonAwsEndpoint);

        [Benchmark]
        public string PreCompiledNonAwsPort() => PreCompiledRegions.GetRegionFromEndpoint(nonAwsEndpointPort);

        [Benchmark]
        public string CurrentAws() => Regions.GetRegionFromEndpoint(awsEndpoint);

        [Benchmark]
        public string CurrentNonAws() => Regions.GetRegionFromEndpoint(nonAwsEndpoint);

        [Benchmark]
        public string CurrentNonAwsPort() => Regions.GetRegionFromEndpoint(nonAwsEndpointPort);
    }
}