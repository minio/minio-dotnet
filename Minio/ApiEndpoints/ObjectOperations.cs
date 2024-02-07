/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2017-2021 MinIO, Inc.
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
using System.Globalization;
using System.Net;
using System.Reactive.Linq;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Minio.DataModel.Result;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "Split up in partial classes")]
public partial class MinioClient : IObjectOperations
{
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
    public Task<ObjectStat> GetObjectAsync(GetObjectArgs args, CancellationToken cancellationToken = default)
    {
        return GetObjectHelper(args, cancellationToken);
    }

    /// <summary>
    ///     Removes an object with given name in specific bucket
    /// </summary>
    /// <param name="args">
    ///     RemoveObjectArgs Arguments Object encapsulates information like - bucket name, object name, optional
    ///     list of versions to be deleted
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Task</returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task RemoveObjectAsync(RemoveObjectArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

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
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task<IObservable<DeleteError>> RemoveObjectsAsync(RemoveObjectsArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        IList<DeleteError> errs = new List<DeleteError>();
        errs = args.ObjectNamesVersions.Count > 0
            ? await RemoveObjectVersionsHelper(args, errs.ToList(), cancellationToken).ConfigureAwait(false)
            : await RemoveObjectsHelper(args, errs, cancellationToken).ConfigureAwait(false);

        return Observable.Create<DeleteError>( // From Current change
            async obs =>
            {
                await Task.Yield();
                foreach (var error in errs) obs.OnNext(error);
            }
        );
    }

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
    public async Task<PutObjectResponse> PutObjectAsync(PutObjectArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();

        var isSnowball = args.Headers.ContainsKey("X-Amz-Meta-Snowball-Auto-Extract") &&
                         Convert.ToBoolean(args.Headers["X-Amz-Meta-Snowball-Auto-Extract"],
                             CultureInfo.InvariantCulture);

        // Upload object in single part if size falls under restricted part size
        // or the request has snowball objects
        if ((args.ObjectSize < Constants.MinimumPartSize || isSnowball) && args.ObjectSize >= 0 &&
            args.ObjectStreamData is not null)
        {
            var bytes = await ReadFullAsync(args.ObjectStreamData, (int)args.ObjectSize).ConfigureAwait(false);
            var bytesRead = bytes.Length;
            if (bytesRead != (int)args.ObjectSize)
                throw new UnexpectedShortReadException(
                    $"Data read {bytesRead.ToString(CultureInfo.InvariantCulture)} is shorter than the size {args.ObjectSize.ToString(CultureInfo.InvariantCulture)} of input buffer.");

            args = args.WithRequestBody(bytes)
                .WithStreamData(null)
                .WithObjectSize(bytesRead);
            return await PutObjectSinglePartAsync(args, cancellationToken, true).ConfigureAwait(false);
        }

        // For all sizes greater than 5MiB do multipart.
        var multipartUploadArgs = new NewMultipartUploadPutArgs()
            .WithBucket(args.BucketName)
            .WithObject(args.ObjectName)
            .WithVersionId(args.VersionId)
            .WithHeaders(args.Headers)
            .WithContentType(args.ContentType)
            .WithLegalHold(args.LegalHoldEnabled);
        // Get upload Id after creating new multi-part upload operation to
        // be used in putobject part, complete multipart upload operations.
        var uploadId = await NewMultipartUploadAsync(multipartUploadArgs, cancellationToken).ConfigureAwait(false);
        var putObjectPartArgs = new PutObjectPartArgs()
            .WithBucket(args.BucketName)
            .WithObject(args.ObjectName)
            .WithObjectSize(args.ObjectSize)
            .WithContentType(args.ContentType)
            .WithUploadId(uploadId)
            .WithStreamData(args.ObjectStreamData)
            .WithProgress(args.Progress)
            .WithRequestBody(args.RequestBody)
            .WithHeaders(args.Headers);
        IDictionary<int, string> etags = null;
        // Upload file contents.
        if (!string.IsNullOrEmpty(args.FileName))
        {
            using var fileStream = new FileStream(args.FileName, FileMode.Open, FileAccess.Read);
            putObjectPartArgs = putObjectPartArgs
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithRequestBody(null);
            etags = await PutObjectPartAsync(putObjectPartArgs, cancellationToken).ConfigureAwait(false);
        }
        // Upload stream contents
        else
        {
            etags = await PutObjectPartAsync(putObjectPartArgs, cancellationToken).ConfigureAwait(false);
        }

        var completeMultipartUploadArgs = new CompleteMultipartUploadArgs()
            .WithBucket(args.BucketName)
            .WithObject(args.ObjectName)
            .WithUploadId(uploadId)
            .WithETags(etags);
        var putObjectResponse = await CompleteMultipartUploadAsync(completeMultipartUploadArgs, cancellationToken)
            .ConfigureAwait(false);
        putObjectResponse.Size = args.ObjectSize;
        return putObjectResponse;
    }

    /// <summary>
    ///     Tests the object's existence and returns metadata about existing objects.
    /// </summary>
    /// <param name="args">
    ///     StatObjectArgs Arguments Object encapsulates information like - bucket name, object name,
    ///     server-side encryption object
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Facts about the object</returns>
    public async Task<ObjectStat> StatObjectAsync(StatObjectArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var responseHeaders = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var param in response.Headers.ToList()) responseHeaders.Add(param.Key, param.Value);
        var statResponse = new StatObjectResponse(response.StatusCode, response.Content, response.Headers, args);

        return statResponse.ObjectInfo;
    }

    /// <summary>
    ///     Get list of multi-part uploads matching particular uploadIdMarker
    /// </summary>
    /// <param name="args">GetMultipartUploadsListArgs Arguments Object which encapsulates bucket name, prefix, recursive</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    private async Task<Tuple<ListMultipartUploadsResult, List<Upload>>> GetMultipartUploadsListAsync(
        GetMultipartUploadsListArgs args,
        CancellationToken cancellationToken)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var getUploadResponse = new GetMultipartUploadsListResponse(response.StatusCode, response.Content);

        return getUploadResponse.UploadResult;
    }

    /// <summary>
    ///     Remove object with matching uploadId from bucket
    /// </summary>
    /// <param name="args">RemoveUploadArgs Arguments Object which encapsulates bucket, object names, upload Id</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    private async Task RemoveUploadAsync(RemoveUploadArgs args, CancellationToken cancellationToken)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Upload object part to bucket for particular uploadId
    /// </summary>
    /// <param name="args">
    ///     PutObjectArgs encapsulates bucket name, object name, upload id, part number, object data(body),
    ///     Headers
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <param name="singleFile">
    ///     This boolean parameter differentiates single part file upload and
    ///     multi part file upload as this function is shared by both.
    /// </param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectDisposedException">The file stream has been disposed</exception>
    /// <exception cref="NotSupportedException">The file stream cannot be read from</exception>
    /// <exception cref="InvalidOperationException">The file stream is currently in a read operation</exception>
    /// <exception cref="AccessDeniedException">For encrypted PUT operation, Access is denied if the key is wrong</exception>
    private async Task<PutObjectResponse> PutObjectSinglePartAsync(PutObjectArgs args,
        CancellationToken cancellationToken = default,
        bool singleFile = false)
    {
        //Skipping validate as we need the case where stream sends 0 bytes
        var progressReport = new ProgressReport();
        if (singleFile) args.Progress?.Report(progressReport);
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        if (singleFile && args.Progress is not null)
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(args.BucketName)
                .WithObject(args.ObjectName);
            var stat = await StatObjectAsync(statArgs, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                progressReport.Percentage = 100;
                progressReport.TotalBytesTransferred = stat.Size;
            }

            args.Progress.Report(progressReport);
        }

        return new PutObjectResponse(response.StatusCode, response.Content, response.Headers,
            args.ObjectSize, args.ObjectName);
    }

    /// <summary>
    ///     Upload object in multiple parts. Private Helper function
    /// </summary>
    /// <param name="args">PutObjectPartArgs encapsulates bucket name, object name, upload id, part number, object data(body)</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectDisposedException">The file stream has been disposed</exception>
    /// <exception cref="NotSupportedException">The file stream cannot be read from</exception>
    /// <exception cref="InvalidOperationException">The file stream is currently in a read operation</exception>
    /// <exception cref="AccessDeniedException">For encrypted PUT operation, Access is denied if the key is wrong</exception>
    private async Task<IDictionary<int, string>> PutObjectPartAsync(PutObjectPartArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var multiPartInfo = Utils.CalculateMultiPartSize(args.ObjectSize);
        var partSize = multiPartInfo.PartSize;
        var partCount = multiPartInfo.PartCount;
        var lastPartSize = multiPartInfo.LastPartSize;
        var totalParts = new Part[(int)partCount];

        var expectedReadSize = partSize;
        int partNumber;
        var numPartsUploaded = 0;
        var etags = new Dictionary<int, string>();
        var progressReport = new ProgressReport();
        args.Progress?.Report(progressReport);
        for (partNumber = 1; partNumber <= partCount; partNumber++)
        {
            var dataToCopy = await ReadFullAsync(args.ObjectStreamData, (int)partSize).ConfigureAwait(false);
            if (dataToCopy.IsEmpty && numPartsUploaded > 0) break;
            if (partNumber == partCount) expectedReadSize = lastPartSize;
            var putObjectArgs = new PutObjectArgs(args)
                .WithRequestBody(dataToCopy)
                .WithUploadId(args.UploadId)
                .WithPartNumber(partNumber);
            var putObjectResponse =
                await PutObjectSinglePartAsync(putObjectArgs, cancellationToken).ConfigureAwait(false);
            var etag = putObjectResponse.Etag;

            numPartsUploaded++;
            totalParts[partNumber - 1] = new Part
            {
                PartNumber = partNumber, ETag = etag, Size = (long)expectedReadSize
            };
            etags[partNumber] = etag;
            if (!dataToCopy.IsEmpty) progressReport.TotalBytesTransferred += dataToCopy.Length;
            if (args.ObjectSize != -1) progressReport.Percentage = (int)(100 * partNumber / partCount);
            args.Progress?.Report(progressReport);
        }

        // This shouldn't happen where stream size is known.
        if (partCount != numPartsUploaded && args.ObjectSize != -1)
        {
            var removeUploadArgs = new RemoveUploadArgs()
                .WithBucket(args.BucketName)
                .WithObject(args.ObjectName)
                .WithUploadId(args.UploadId);
            await RemoveUploadAsync(removeUploadArgs, cancellationToken).ConfigureAwait(false);
            return null;
        }

        return etags;
    }

    /// <summary>
    ///     Start a new multi-part upload request
    /// </summary>
    /// <param name="args">
    ///     NewMultipartUploadPutArgs arguments object encapsulating bucket name, object name, Headers
    ///     Headers
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
    private async Task<string> NewMultipartUploadAsync(NewMultipartUploadPutArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var uploadResponse = new NewMultipartUploadResponse(response.StatusCode, response.Content);
        return uploadResponse.UploadId;
    }

    /// <summary>
    ///     Start a new multi-part copy upload request
    /// </summary>
    /// <param name="args">
    ///     NewMultipartUploadCopyArgs arguments object encapsulating bucket name, object name, Headers
    ///     Headers
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
    private async Task<string> NewMultipartUploadAsync(NewMultipartUploadCopyArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var uploadResponse = new NewMultipartUploadResponse(response.StatusCode, response.Content);
        return uploadResponse.UploadId;
    }

    /// <summary>
    ///     Internal method to complete multi part upload of object to server.
    /// </summary>
    /// <param name="args">CompleteMultipartUploadArgs Arguments object with bucket name, object name, upload id, Etags</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
    private async Task<PutObjectResponse> CompleteMultipartUploadAsync(CompleteMultipartUploadArgs args,
        CancellationToken cancellationToken)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        return new PutObjectResponse(response.StatusCode, response.Content, response.Headers, -1,
            args.ObjectName);
    }

    /// <summary>
    ///     Advances in the stream upto currentPartSize or End of Stream
    /// </summary>
    /// <param name="data"></param>
    /// <param name="currentPartSize"></param>
    /// <returns>bytes read in a byte array</returns>
    internal async Task<ReadOnlyMemory<byte>> ReadFullAsync(Stream data, int currentPartSize)
    {
        Memory<byte> result = new byte[currentPartSize];
        var totalRead = 0;
        while (totalRead < currentPartSize)
        {
            Memory<byte> curData = new byte[currentPartSize - totalRead];
            var curRead = await data.ReadAsync(curData[..(currentPartSize - totalRead)]).ConfigureAwait(false);
            if (curRead == 0) break;
            for (var i = 0; i < curRead; i++)
                curData.Slice(i, 1).CopyTo(result[(totalRead + i)..]);
            totalRead += curRead;
        }

        if (totalRead == 0) return null;

        if (totalRead == currentPartSize) return result;

        Memory<byte> truncatedResult = new byte[totalRead];
        for (var i = 0; i < totalRead; i++)
            result.Slice(i, 1).CopyTo(truncatedResult[i..]);
        return truncatedResult;
    }
}
