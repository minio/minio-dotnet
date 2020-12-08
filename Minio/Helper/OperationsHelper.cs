/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using Minio.Exceptions;

namespace Minio
{
    public partial class MinioClient : IObjectOperations
    {
        /// <summary>
        /// private helper method to remove list of objects from bucket
        /// </summary>
        /// <param name="args">RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects, optional list of versions (for each object) to be deleted</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
    	private async Task<List<DeleteError>> removeObjectsAsync(RemoveObjectsArgs args, CancellationToken cancellationToken)
        {
            var request = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            RemoveObjectsResponse removeObjectsResponse = new RemoveObjectsResponse(response.StatusCode, response.Content);
            var deleteErrorList = new List<DeleteError>();
            if (removeObjectsResponse.DeletedObjectsResult != null)
            {
                deleteErrorList = removeObjectsResponse.DeletedObjectsResult.errorList;
            }
            return deleteErrorList;
        }

        /// <summary>
        /// private helper method to remove 1000 objects from bucket
        /// </summary>
        /// <param name="args">RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects, optional list of versions (for each object) to be deleted</param>
        /// <param name="objVersions">List of Tuples. Each tuple is Object name to List of Version IDs to be deleted</param>
        /// <param name="fullErrorsList">Full List of DeleteError objects. The error list from this call will be added to the full list.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<List<DeleteError>> callRemoveObjectVersions(RemoveObjectsArgs args, List<Tuple<string, string>> objVersions, List<DeleteError> fullErrorsList, CancellationToken cancellationToken)
        {
            RemoveObjectsArgs iterArgs = new RemoveObjectsArgs()
                                                .WithBucket(args.BucketName)
                                                .WithObjectsVersions(objVersions);
            var errorsList = await removeObjectsAsync(iterArgs, cancellationToken).ConfigureAwait(false);
            fullErrorsList.AddRange(errorsList);
            return fullErrorsList;
        }


        /// <summary>
        /// private helper method to remove 1000 objects from bucket
        /// </summary>
        /// <param name="args">RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects, optional list of versions (for each object) to be deleted</param>
        /// <param name="objNames">List of Object names to be deleted</param>
        /// <param name="fullErrorsList">Full List of DeleteError objects. The error list from this call will be added to the full list.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<List<DeleteError>> callRemoveObjects(RemoveObjectsArgs args, List<string> objNames, List<DeleteError> fullErrorsList, CancellationToken cancellationToken)
        {
            RemoveObjectsArgs iterArgs = new RemoveObjectsArgs()
                                                .WithBucket(args.BucketName)
                                                .WithObjects(objNames);
            var errorsList = await removeObjectsAsync(iterArgs, cancellationToken).ConfigureAwait(false);
            fullErrorsList.AddRange(errorsList);
            return fullErrorsList;
        }


        private async Task<List<DeleteError>> removeObjectVersionsHelper(RemoveObjectsArgs args, List<DeleteError> fullErrorsList, CancellationToken cancellationToken)
        {
            int i = 0;
            if (args.ObjectNamesVersions.Count <= 1000)
            {
                fullErrorsList.AddRange(await callRemoveObjectVersions(args, args.ObjectNamesVersions, fullErrorsList, cancellationToken));
                return fullErrorsList;
            }
            else
            {
                List<Tuple<string, string>> curItemList = new List<Tuple<string, string>>(args.ObjectNamesVersions.GetRange(0, 1000));
                int curItemListCount = curItemList.Count;
                int deletedCount = 0;
                while (curItemListCount > 0)
                {
                    Console.WriteLine("curItemList.Count " + curItemList.Count);
                    var errorList = await callRemoveObjectVersions(args, curItemList, fullErrorsList, cancellationToken).ConfigureAwait(false);
                    deletedCount += curItemList.Count;
                    fullErrorsList.AddRange(errorList);
                    curItemList.Clear();
                    if ((args.ObjectNamesVersions.Count - deletedCount) <= 0)
                    {
                        break;
                    }
                    if ((args.ObjectNamesVersions.Count - deletedCount) <= 1000)
                    {
                        curItemList.AddRange(args.ObjectNamesVersions.GetRange(curItemListCount, (args.ObjectNamesVersions.Count - deletedCount)));
                    }
                    else
                    {
                        curItemList.AddRange(args.ObjectNamesVersions.GetRange(curItemListCount, 1000));
                    }
                    curItemListCount = curItemList.Count;
                }
            }
            return fullErrorsList;
        }
        private async Task<List<DeleteError>> removeObjectsHelper(RemoveObjectsArgs args, List<DeleteError> fullErrorsList, CancellationToken cancellationToken)
        {
            List<string> iterObjects = new List<string>();
            int i =0;
            foreach(var objName in args.ObjectNames)
            {
                utils.ValidateObjectName(objName);
                iterObjects.Add(objName);
                i++;
                if ((i % 1000) == 0)
                {
                    fullErrorsList = await callRemoveObjects(args, iterObjects, fullErrorsList, cancellationToken);
                    iterObjects.Clear();
                }
            }
            if (iterObjects.Count > 0)
            {
                fullErrorsList = await callRemoveObjects(args, iterObjects, fullErrorsList, cancellationToken);
            }
            return fullErrorsList;
        }
    }

    public class OperationsUtil
    {
        private static readonly List<string> SupportedHeaders = new List<string> { "cache-control", "content-encoding", "content-type", "x-amz-acl", "content-disposition" };
        internal static bool IsSupportedHeader(string hdr, IEqualityComparer<string> comparer = null)
        {
            comparer = comparer ?? StringComparer.OrdinalIgnoreCase;
            return SupportedHeaders.Contains(hdr, comparer);
        }
    }

}