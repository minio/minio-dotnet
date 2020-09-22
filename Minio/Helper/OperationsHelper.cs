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
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
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
        private async Task<List<DeleteError>> callRemoveObjectVersions(RemoveObjectsArgs args, List<Tuple<string, List<string>>> objVersions, List<DeleteError> fullErrorsList, CancellationToken cancellationToken)
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
            int listIndex = 0;
            int i = 0;
            List<Tuple<string, List<string>>> objVersions = new List<Tuple<string, List<string>>>();

            foreach(var objVerTuple in args.ObjectNamesVersions)
            {
                utils.ValidateObjectName(objVerTuple.Item1);
                if ((listIndex + objVerTuple.Item2.Count) <= 1000)
                {
                    objVersions.Add(objVerTuple);
                    i += objVerTuple.Item2.Count;
                    listIndex += objVerTuple.Item2.Count;
                    if (listIndex == 1000)
                    {
                        listIndex = 0;
                        fullErrorsList.AddRange(await callRemoveObjectVersions(args, objVersions, fullErrorsList, cancellationToken));
                        objVersions.Clear();
                    }
                }
                // Once we make the iteration count for remove as 1000.
                // Check what is remaining in the tuple list.
                // Remaining is more than 1000.
                else if ((listIndex + objVerTuple.Item2.Count) > 1000)
                {
                    List<string> curItemList = new List<string>();
                    curItemList.AddRange(objVerTuple.Item2);
                    string objectName = objVerTuple.Item1;
                    List<string> objVersionList = new List<string>();
                    // Fill until count is 1000.
                    objVersionList.AddRange(curItemList.GetRange(0, (1000 - (listIndex + 1))));
                    curItemList.RemoveRange(0, (1000 - (listIndex + 1)));
                    objVersions.Add(new Tuple<string, List<string>>(objectName, objVersionList));
                    // objectVersions has 1000 <object-name, version id> pairs now. Call Remove.
                    var errorList = await callRemoveObjectVersions(args, objVersions, fullErrorsList, cancellationToken).ConfigureAwait(false);
                    fullErrorsList.AddRange(errorList);
                    int curItemListCount = curItemList.Count;
                    // Since we only get this one iteration, we'll empty the rest of the items in the list in batches of 1000 or less.
                    while (curItemListCount > 0)
                    {
                        Tuple<string, List<string>> tpl;
                        if (curItemListCount >= 1000)
                        {
                            tpl = new Tuple<string, List<string>>(objVerTuple.Item1, curItemList.GetRange(0, 1000));
                            objVersions.Add(tpl);
                            curItemList.RemoveRange(0, 1000);
                            curItemListCount = curItemList.Count;
                        }
                        else
                        {
                            tpl = new Tuple<string, List<string>>(objVerTuple.Item1, curItemList.GetRange(0, curItemListCount));
                            objVersions.Add(tpl);
                            curItemList.Clear();
                            curItemListCount = 0;
                        }
                        if (objVersions.Count > 0)
                        {
                            errorList = await callRemoveObjectVersions(args, objVersions, fullErrorsList, cancellationToken).ConfigureAwait(false);
                            fullErrorsList.AddRange(errorList);
                        }
                    }
                }
            }
            if (objVersions.Count > 0)
            {
                fullErrorsList.AddRange(await callRemoveObjectVersions(args, objVersions, fullErrorsList, cancellationToken));
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
}