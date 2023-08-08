/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020, 2021 MinIO, Inc.
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

using Minio.DataModel.Encryption;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio.DataModel.Args;

public class GetObjectArgs : ObjectConditionalQueryArgs<GetObjectArgs>
{
    public GetObjectArgs()
    {
        RequestMethod = HttpMethod.Get;
        OffsetLengthSet = false;
    }

    internal Action<Stream> CallBack { get; private set; }
    internal Func<Stream, CancellationToken, Task> FuncCallBack { get; private set; }
    internal long ObjectOffset { get; private set; }
    internal long ObjectLength { get; private set; }
    internal string FileName { get; private set; }
    internal bool OffsetLengthSet { get; set; }

    internal override void Validate()
    {
        base.Validate();
        if (CallBack is null && FuncCallBack is null && string.IsNullOrEmpty(FileName))
            throw new MinioException("Atleast one of " + nameof(CallBack) + ", CallBack method or " + nameof(FileName) +
                                     " file path to save need to be set for GetObject operation.");

        if (OffsetLengthSet)
        {
            if (ObjectOffset < 0) throw new InvalidDataException("Offset should be zero or greater: " + nameof(ObjectOffset));

            if (ObjectLength < 0)
                throw new InvalidDataException("Length should be greater than or equal to zero: " + nameof(ObjectLength));
        }

        if (FileName is not null) Utils.ValidateFile(FileName);
        Populate();
    }

    private void Populate()
    {
        Headers ??= new Dictionary<string, string>(StringComparer.Ordinal);
        if (SSE?.GetEncryptionType().Equals(EncryptionType.SSE_C) == true) SSE.Marshal(Headers);

        if (OffsetLengthSet)
        {
            // "Range" header accepts byte start index and end index
            if (ObjectLength > 0 && ObjectOffset > 0)
                Headers["Range"] = "bytes=" + ObjectOffset + "-" + (ObjectOffset + ObjectLength - 1);
            else if (ObjectLength == 0 && ObjectOffset > 0)
                Headers["Range"] = "bytes=" + ObjectOffset + "-";
            else if (ObjectLength > 0 && ObjectOffset == 0) Headers["Range"] = "bytes=0-" + (ObjectLength - 1);
        }
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", $"{VersionId}");

        if (CallBack is not null) requestMessageBuilder.ResponseWriter = CallBack;
        else requestMessageBuilder.FunctionResponseWriter = FuncCallBack;

        if (Headers.TryGetValue(S3ZipExtractKey, out var value))
            requestMessageBuilder.AddQueryParameter(S3ZipExtractKey, value);

        return requestMessageBuilder;
    }

    public GetObjectArgs WithCallbackStream(Action<Stream> cb)
    {
        CallBack = cb;
        return this;
    }

    public GetObjectArgs WithCallbackStream(Func<Stream, CancellationToken, Task> cb)
    {
        FuncCallBack = cb;
        return this;
    }

    public GetObjectArgs WithOffsetAndLength(long offset, long length)
    {
        OffsetLengthSet = true;
        ObjectOffset = offset < 0 ? 0 : offset;
        ObjectLength = length < 0 ? 0 : length;
        return this;
    }

    public GetObjectArgs WithLength(long length)
    {
        OffsetLengthSet = true;
        ObjectOffset = 0;
        ObjectLength = length < 0 ? 0 : length;
        return this;
    }

    public GetObjectArgs WithFile(string file)
    {
        FileName = file;
        return this;
    }
}
