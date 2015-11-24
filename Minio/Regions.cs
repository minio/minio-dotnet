/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2015 Minio, Inc.
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

namespace Minio
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
                case "s3-ap-northeast-1.amazonaws.com":
                    return "ap-northeast-1";
                case "s3-ap-southeast-1.amazonaws.com":
                    return "ap-southeast-1";
                case "s3-ap-southeast-2.amazonaws.com":
                    return "ap-southeast-2";
                case "s3-eu-central-1.amazonaws.com":
                    return "eu-central-1";
                case "s3-eu-west-1.amazonaws.com":
                    return "eu-west-1";
                case "s3-sa-east-1.amazonaws.com":
                    return "sa-east-1";
                case "s3.amazonaws.com":
                    return "us-east-1";
                case "s3-us-west-1.amazonaws.com":
                    return "us-west-1";
                case "s3-us-west-2.amazonaws.com":
                    return "us-west-2";
                default:
                    return "us-east-1";
            }
        }
    }
}
