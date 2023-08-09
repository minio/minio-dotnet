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

using System.Security.Cryptography;
using Minio.Helper;

namespace Minio.DataModel.Args;

public class PutObjectArgs : ObjectWriteArgs<PutObjectArgs>
{
    public PutObjectArgs()
    {
        RequestMethod = HttpMethod.Put;
        RequestBody = null;
        ObjectStreamData = null;
        PartNumber = 0;
        ContentType = "application/octet-stream";
    }

    internal PutObjectArgs(PutObjectPartArgs args)
    {
        RequestMethod = HttpMethod.Put;
        BucketName = args.BucketName;
        ContentType = args.ContentType ?? "application/octet-stream";
        FileName = args.FileName;
        Headers = args.Headers;
        ObjectName = args.ObjectName;
        ObjectSize = args.ObjectSize;
        PartNumber = args.PartNumber;
        SSE = args.SSE;
        UploadId = args.UploadId;
    }

    internal string UploadId { get; private set; }
    internal int PartNumber { get; set; }
    internal string FileName { get; set; }
    internal long ObjectSize { get; set; }
    internal Stream ObjectStreamData { get; set; }
    internal IProgress<ProgressReport> Progress { get; set; }

    internal override void Validate()
    {
        base.Validate();
        // Check atleast one of filename or stream are initialized
        if (string.IsNullOrWhiteSpace(FileName) && ObjectStreamData is null)
            throw new InvalidOperationException("One of " + nameof(FileName) + " or " + nameof(ObjectStreamData) +
                                                " must be set.");

        if (PartNumber < 0)
            throw new InvalidDataException("Invalid Part number value. Cannot be less than 0");
        // Check if only one of filename or stream are initialized
        if (!string.IsNullOrWhiteSpace(FileName) && ObjectStreamData is not null)
            throw new InvalidOperationException("Only one of " + nameof(FileName) + " or " + nameof(ObjectStreamData) +
                                                " should be set.");

        if (!string.IsNullOrWhiteSpace(FileName)) Utils.ValidateFile(FileName);
        // Check object size when using stream data
        if (ObjectStreamData is not null && ObjectSize == 0)
            throw new InvalidOperationException($"{nameof(ObjectSize)} must be set");
        Populate();
    }

    private void Populate()
    {
        if (!string.IsNullOrWhiteSpace(FileName))
        {
            var fileInfo = new FileInfo(FileName);
            ObjectSize = fileInfo.Length;
            ObjectStreamData = new FileStream(FileName, FileMode.Open, FileAccess.Read);
        }
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder = base.BuildRequest(requestMessageBuilder);
        if (string.IsNullOrWhiteSpace(ContentType)) ContentType = "application/octet-stream";
        if (!Headers.ContainsKey("Content-Type")) Headers["Content-Type"] = ContentType;

        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Type", Headers["Content-Type"]);
        if (!string.IsNullOrWhiteSpace(UploadId) && PartNumber > 0)
        {
            requestMessageBuilder.AddQueryParameter("uploadId", $"{UploadId}");
            requestMessageBuilder.AddQueryParameter("partNumber", $"{PartNumber}");
        }

        if (ObjectTags?.TaggingSet?.Tag.Count > 0)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging", ObjectTags.GetTagString());

        if (Retention is not null)
        {
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                Retention.RetainUntilDate);
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode", Retention.Mode.ToString());
            requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
                Utils.GetMD5SumStr(RequestBody.Span));
        }

        if (LegalHoldEnabled is not null)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-legal-hold",
                LegalHoldEnabled == true ? "ON" : "OFF");

        if (!RequestBody.IsEmpty)
        {
#if NETSTANDARD
            using var sha = SHA256.Create();
            var hash
                = sha.ComputeHash(RequestBody.ToArray());
#else
            var hash = SHA256.HashData(RequestBody.Span);
#endif
            var hex = BitConverter.ToString(hash).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
                .ToLowerInvariant();
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", hex);
            requestMessageBuilder.SetBody(RequestBody);
        }

        return requestMessageBuilder;
    }

    public override PutObjectArgs WithHeaders(IDictionary<string, string> headers)
    {
        Headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (headers is not null)
            foreach (var p in headers)
            {
                var key = p.Key;
                if (!OperationsUtil.IsSupportedHeader(p.Key) &&
                    !p.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase) &&
                    !OperationsUtil.IsSSEHeader(p.Key))
                {
                    key = "x-amz-meta-" + key.ToLowerInvariant();
                    _ = Headers.Remove(p.Key);
                }

                Headers[key] = p.Value;
                if (string.Equals(key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    ContentType = p.Value;
            }

        if (string.IsNullOrWhiteSpace(ContentType)) ContentType = "application/octet-stream";
        if (!Headers.ContainsKey("Content-Type")) Headers["Content-Type"] = ContentType;
        return this;
    }

    internal PutObjectArgs WithUploadId(string id = null)
    {
        UploadId = id;
        return this;
    }

    internal PutObjectArgs WithPartNumber(int num)
    {
        PartNumber = num;
        return this;
    }

    public PutObjectArgs WithFileName(string file)
    {
        FileName = file;
        return this;
    }

    public PutObjectArgs WithObjectSize(long size)
    {
        ObjectSize = size;
        return this;
    }

    public PutObjectArgs WithStreamData(Stream data)
    {
        ObjectStreamData = data;
        return this;
    }

    public PutObjectArgs WithProgress(IProgress<ProgressReport> progress)
    {
        Progress = progress;
        return this;
    }
}
