/*
 * Newtera .NET Library for Newtera TDM,
 * (C) 2017-2021 Newtera, Inc.
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

using Newtera.DataModel;
using Newtera.DataModel.Args;
using Newtera.DataModel.Response;
using Newtera.Exceptions;

namespace Newtera.ApiEndpoints;

public interface IObjectOperations
{
    /// <summary>
    ///     Removes an object with given name in specific bucket
    /// </summary>
    /// <param name="args">
    ///     RemoveObjectArgs Arguments Object encapsulates information like - bucket name, object name, whether
    ///     delete all versions
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    Task RemoveObjectAsync(RemoveObjectArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes list of objects from bucket
    /// </summary>
    /// <param name="args">
    ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
    ///     optional list of versions (for each object) to be deleted
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Observable that returns delete error while deleting objects if any</returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    Task<IObservable<DeleteError>> RemoveObjectsAsync(RemoveObjectsArgs args,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get an object. The object will be streamed to the callback given by the user.
    /// </summary>
    /// <param name="args">
    ///     GetObjectArgs Arguments Object encapsulates information like - bucket name, object name, server-side
    ///     encryption object, action stream, length, offset
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="DirectoryNotFoundException">If the directory to copy to is not found</exception>
    Task<ObjectStat> GetObjectAsync(GetObjectArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates object in a bucket fom input stream or filename.
    /// </summary>
    /// <param name="args">
    ///     PutObjectArgs Arguments object encapsulating bucket name, object name, file name, object data
    ///     stream, object size, content type.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="FileNotFoundException">If the file to copy from not found</exception>
    /// <exception cref="ObjectDisposedException">The file stream has been disposed</exception>
    /// <exception cref="NotSupportedException">The file stream cannot be read from</exception>
    /// <exception cref="InvalidOperationException">The file stream is currently in a read operation</exception>
    /// <exception cref="AccessDeniedException">For encrypted PUT operation, Access is denied if the key is wrong</exception>
    Task<PutObjectResponse> PutObjectAsync(PutObjectArgs args, CancellationToken cancellationToken = default);
}
