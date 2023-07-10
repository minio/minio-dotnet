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

using System.Diagnostics.CodeAnalysis;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "Split up in partial classes")]
public partial class MinioClient : IMinioClient
{
    /// <summary>
    ///     private helper method to remove list of objects from bucket
    /// </summary>
    /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name etc </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    private async Task<ObjectStat> GetObjectHelper(GetObjectArgs args, CancellationToken cancellationToken = default)
    {
        // StatObject is called to both verify the existence of the object and return it with GetObject.
        // NOTE: This avoids writing the error body to the action stream passed (Do not remove).

        var statArgs = new StatObjectArgs()
            .WithBucket(args.BucketName)
            .WithObject(args.ObjectName)
            .WithVersionId(args.VersionId)
            .WithMatchETag(args.MatchETag)
            .WithNotMatchETag(args.NotMatchETag)
            .WithModifiedSince(args.ModifiedSince)
            .WithUnModifiedSince(args.UnModifiedSince)
            .WithServerSideEncryption(args.SSE)
            .WithHeaders(args.Headers);
        if (args.OffsetLengthSet) statArgs.WithOffsetAndLength(args.ObjectOffset, args.ObjectLength);
        var objStat = await StatObjectAsync(statArgs, cancellationToken).ConfigureAwait(false);
        args?.Validate();
        if (args.FileName is not null)
            await GetObjectFileAsync(args, objStat, cancellationToken).ConfigureAwait(false);
        else if (args.CallBack is not null)
            await GetObjectStreamAsync(args, objStat, args.CallBack, cancellationToken).ConfigureAwait(false);
        else await GetObjectStreamAsync(args, objStat, args.FuncCallBack, cancellationToken).ConfigureAwait(false);
        return objStat;
    }

    /// <summary>
    ///     private helper method return the specified object from the bucket
    /// </summary>
    /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name etc </param>
    /// <param name="objectStat"> ObjectStat object encapsulates information like - object name, size, etag etc </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    private Task GetObjectFileAsync(GetObjectArgs args, ObjectStat objectStat,
        CancellationToken cancellationToken = default)
    {
        var length = objectStat.Size;
        var etag = objectStat.ETag;

        var tempFileName = $"{args.FileName}.{etag}.part.minio";
        if (!string.IsNullOrEmpty(args.VersionId)) tempFileName = $"{args.FileName}.{etag}.{args.VersionId}.part.minio";
        if (File.Exists(args.FileName)) File.Delete(args.FileName);

        Utils.ValidateFile(tempFileName);
        if (File.Exists(tempFileName)) File.Delete(tempFileName);

        var callbackAsync = async (Stream stream, CancellationToken cancellationToken) =>
        {
            using var dest = new FileStream(tempFileName, FileMode.Create, FileAccess.Write);
#if NETSTANDARD
            await stream.CopyToAsync(dest).ConfigureAwait(false);
#else
            await stream.CopyToAsync(dest, cancellationToken).ConfigureAwait(false);
#endif
        };

#pragma warning disable IDISP001 // Dispose created
        var cts = new CancellationTokenSource();
#pragma warning restore IDISP001 // Dispose created
        cts.CancelAfter(TimeSpan.FromSeconds(15));
        args.WithCallbackStream(async (stream, cancellationToken) =>
        {
            await callbackAsync(stream, cts.Token).ConfigureAwait(false);
            Utils.MoveWithReplace(tempFileName, args.FileName);
        });
        return GetObjectStreamAsync(args, objectStat, null, cancellationToken);
    }

    /// <summary>
    ///     private helper method. It returns the specified portion or full object from the bucket
    /// </summary>
    /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name etc </param>
    /// <param name="objectStat">
    ///     ObjectStat object encapsulates information like - object name, size, etag etc, represents
    ///     Object Information
    /// </param>
    /// <param name="cb"> Action object of type Stream, callback to send Object contents, if assigned </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    private async Task GetObjectStreamAsync(GetObjectArgs args, ObjectStat objectStat, Action<Stream> cb,
        CancellationToken cancellationToken = default)
    {
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response =
            await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     private helper method. It returns the specified portion or full object from the bucket
    /// </summary>
    /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name etc </param>
    /// <param name="objectStat">
    ///     ObjectStat object encapsulates information like - object name, size, etag etc, represents
    ///     Object Information
    /// </param>
    /// <param name="cb">
    ///     Callback function to send/process Object contents using
    ///     async Func object which takes Stream and CancellationToken as input
    ///     and Task as output, if assigned
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    private async Task GetObjectStreamAsync(GetObjectArgs args, ObjectStat objectStat,
        Func<Stream, CancellationToken, Task> cb,
        CancellationToken cancellationToken = default)
    {
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response =
            await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     private helper method to remove list of objects from bucket
    /// </summary>
    /// <param name="args">
    ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
    ///     optional list of versions (for each object) to be deleted
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    private async Task<IList<DeleteError>> removeObjectsAsync(RemoveObjectsArgs args,
        CancellationToken cancellationToken)
    {
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response =
            await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var removeObjectsResponse = new RemoveObjectsResponse(response.StatusCode, response.Content);
        return removeObjectsResponse.DeletedObjectsResult.ErrorList;
    }

    /// <summary>
    ///     private helper method to call remove objects function
    /// </summary>
    /// <param name="args">
    ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
    ///     optional version Id list
    /// </param>
    /// <param name="objVersions">List of Tuples. Each tuple is Object name to List of Version IDs to be deleted</param>
    /// <param name="fullErrorsList">
    ///     Full List of DeleteError objects. The error list from this call will be added to the full
    ///     list.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    private async Task<IList<DeleteError>> CallRemoveObjectVersions(RemoveObjectsArgs args,
        IList<Tuple<string, string>> objVersions, List<DeleteError> fullErrorsList, CancellationToken cancellationToken)
    {
        var iterArgs = new RemoveObjectsArgs()
            .WithBucket(args.BucketName)
            .WithObjectsVersions(objVersions);
        var errorsList = await removeObjectsAsync(iterArgs, cancellationToken).ConfigureAwait(false);
        fullErrorsList.AddRange(errorsList);
        return fullErrorsList;
    }

    /// <summary>
    ///     private helper method to call function to remove objects/version items in iterations of 1000 each from bucket
    /// </summary>
    /// <param name="args">
    ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
    ///     optional list of versions (for each object) to be deleted
    /// </param>
    /// <param name="objNames">List of Object names to be deleted</param>
    /// <param name="fullErrorsList">
    ///     Full List of DeleteError objects. The error list from this call will be added to the full
    ///     list.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    private async Task<IList<DeleteError>> CallRemoveObjects(RemoveObjectsArgs args, IList<string> objNames,
        List<DeleteError> fullErrorsList, CancellationToken cancellationToken)
    {
        // var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        var iterArgs = new RemoveObjectsArgs()
            .WithBucket(args.BucketName)
            .WithObjects(objNames);
        var errorsList = await removeObjectsAsync(iterArgs, cancellationToken).ConfigureAwait(false);
        fullErrorsList.AddRange(errorsList);
        return fullErrorsList;
    }

    /// <summary>
    ///     private helper method to remove objects/version items in iterations of 1000 each from bucket
    /// </summary>
    /// <param name="args">
    ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
    ///     optional list of versions (for each object) to be deleted
    /// </param>
    /// <param name="fullErrorsList">
    ///     Full List of DeleteError objects. The error list from this call will be added to the full
    ///     list.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    private async Task<IList<DeleteError>> RemoveObjectVersionsHelper(RemoveObjectsArgs args,
        List<DeleteError> fullErrorsList, CancellationToken cancellationToken)
    {
        if (args.ObjectNamesVersions.Count <= 1000)
        {
            fullErrorsList.AddRange(await CallRemoveObjectVersions(args, args.ObjectNamesVersions, fullErrorsList,
                cancellationToken).ConfigureAwait(false));
            return fullErrorsList;
        }

        var curItemList = new List<Tuple<string, string>>(args.ObjectNamesVersions.GetRange(0, 1000));
        var delVersionNextIndex = curItemList.Count;
        var deletedCount = 0;
        while (delVersionNextIndex <= args.ObjectNamesVersions.Count)
        {
            var errorList = await CallRemoveObjectVersions(args, curItemList, fullErrorsList, cancellationToken)
                .ConfigureAwait(false);
            if (delVersionNextIndex == args.ObjectNamesVersions.Count)
                break;
            deletedCount += curItemList.Count;
            fullErrorsList.AddRange(errorList);
            curItemList.Clear();
            if (args.ObjectNamesVersions.Count - delVersionNextIndex <= 1000)
            {
                curItemList.AddRange(args.ObjectNamesVersions.GetRange(delVersionNextIndex,
                    args.ObjectNamesVersions.Count - delVersionNextIndex));
                delVersionNextIndex = args.ObjectNamesVersions.Count;
            }
            else
            {
                curItemList.AddRange(args.ObjectNamesVersions.GetRange(delVersionNextIndex, 1000));
                delVersionNextIndex += 1000;
            }
        }

        return fullErrorsList;
    }

    /// <summary>
    ///     private helper method to remove objects in iterations of 1000 each from bucket
    /// </summary>
    /// <param name="args">
    ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
    ///     optional list of versions (for each object) to be deleted
    /// </param>
    /// <param name="fullErrorsList">
    ///     Full List of DeleteError objects. The error list from this call will be added to the full
    ///     list.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    private async Task<IList<DeleteError>> RemoveObjectsHelper(RemoveObjectsArgs args,
        IList<DeleteError> fullErrorsList,
        CancellationToken cancellationToken)
    {
        var iterObjects = new List<string>(1000);
        var i = 0;
        foreach (var objName in args.ObjectNames)
        {
            Utils.ValidateObjectName(objName);
            iterObjects.Insert(i, objName);
            if (++i == 1000)
            {
                fullErrorsList = await CallRemoveObjects(args, iterObjects, fullErrorsList.ToList(), cancellationToken)
                    .ConfigureAwait(false);
                iterObjects.Clear();
                i = 0;
            }
        }

        if (iterObjects.Count > 0)
            fullErrorsList = await CallRemoveObjects(args, iterObjects, fullErrorsList.ToList(), cancellationToken)
                .ConfigureAwait(false);
        return fullErrorsList;
    }
}
