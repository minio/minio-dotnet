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
using System;
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio
{
    public interface IBucketOperations
    {

        Task<bool> MakeBucketAsync(string bucketName, string location = "us-east-1");

        Task<ListAllMyBucketsResult> ListBucketsAsync();

        Task<bool> BucketExistsAsync(string bucketName);

        Task RemoveBucketAsync(string bucketName);
       // IObservable<Item> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = true);

        Task<PolicyType> GetPolicyAsync(String bucketName, String objectPrefix);

        Task SetPolicyAsync(String bucketName, String objectPrefix, PolicyType policyType);
        
    }
}