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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Minio.DataModel;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Tags;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio;

public class SelectObjectContentArgs : EncryptionArgs<SelectObjectContentArgs>
{
    private readonly SelectObjectOptions SelectOptions;

    public SelectObjectContentArgs()
    {
        RequestMethod = HttpMethod.Post;
        SelectOptions = new SelectObjectOptions();
    }

    internal override void Validate()
    {
        base.Validate();
        if (string.IsNullOrEmpty(SelectOptions.Expression))
            throw new InvalidOperationException("The Expression " + nameof(SelectOptions.Expression) +
                                                " for Select Object Content cannot be empty.");
        if (SelectOptions.InputSerialization == null || SelectOptions.OutputSerialization == null)
            throw new InvalidOperationException(
                "The Input/Output serialization members for SelectObjectContentArgs should be initialized " +
                nameof(SelectOptions.InputSerialization) + " " + nameof(SelectOptions.OutputSerialization));
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("select", "");
        requestMessageBuilder.AddQueryParameter("select-type", "2");

        if (RequestBody == null)
        {
            RequestBody = Encoding.UTF8.GetBytes(SelectOptions.MarshalXML());
            requestMessageBuilder.SetBody(RequestBody);
        }

        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            utils.getMD5SumStr(RequestBody));

        return requestMessageBuilder;
    }

    public SelectObjectContentArgs WithExpressionType(QueryExpressionType e)
    {
        SelectOptions.ExpressionType = e;
        return this;
    }

    public SelectObjectContentArgs WithQueryExpression(string expr)
    {
        SelectOptions.Expression = expr;
        return this;
    }

    public SelectObjectContentArgs WithInputSerialization(SelectObjectInputSerialization serialization)
    {
        SelectOptions.InputSerialization = serialization;
        return this;
    }

    public SelectObjectContentArgs WithOutputSerialization(SelectObjectOutputSerialization serialization)
    {
        SelectOptions.OutputSerialization = serialization;
        return this;
    }

    public SelectObjectContentArgs WithRequestProgress(RequestProgress requestProgress)
    {
        SelectOptions.RequestProgress = requestProgress;
        return this;
    }
}

public class ListIncompleteUploadsArgs : BucketArgs<ListIncompleteUploadsArgs>
{
    public ListIncompleteUploadsArgs()
    {
        RequestMethod = HttpMethod.Get;
        Recursive = true;
    }

    internal string Prefix { get; private set; }
    internal string Delimiter { get; private set; }
    internal bool Recursive { get; private set; }

    public ListIncompleteUploadsArgs WithPrefix(string prefix)
    {
        Prefix = prefix ?? string.Empty;
        return this;
    }

    public ListIncompleteUploadsArgs WithDelimiter(string delim)
    {
        Delimiter = delim ?? string.Empty;
        return this;
    }

    public ListIncompleteUploadsArgs WithRecursive(bool recursive)
    {
        Recursive = recursive;
        Delimiter = recursive ? string.Empty : "/";
        return this;
    }
}

public class GetMultipartUploadsListArgs : BucketArgs<GetMultipartUploadsListArgs>
{
    public GetMultipartUploadsListArgs()
    {
        RequestMethod = HttpMethod.Get;
        MAX_UPLOAD_COUNT = 1000;
    }

    internal string Prefix { get; private set; }
    internal string Delimiter { get; private set; }
    internal string KeyMarker { get; private set; }
    internal string UploadIdMarker { get; private set; }
    internal uint MAX_UPLOAD_COUNT { get; }

    public GetMultipartUploadsListArgs WithPrefix(string prefix)
    {
        Prefix = prefix ?? string.Empty;
        return this;
    }

    public GetMultipartUploadsListArgs WithDelimiter(string delim)
    {
        Delimiter = delim ?? string.Empty;
        return this;
    }

    public GetMultipartUploadsListArgs WithKeyMarker(string nextKeyMarker)
    {
        KeyMarker = nextKeyMarker ?? string.Empty;
        return this;
    }

    public GetMultipartUploadsListArgs WithUploadIdMarker(string nextUploadIdMarker)
    {
        UploadIdMarker = nextUploadIdMarker ?? string.Empty;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploads", "");
        requestMessageBuilder.AddQueryParameter("prefix", Prefix);
        requestMessageBuilder.AddQueryParameter("delimiter", Delimiter);
        requestMessageBuilder.AddQueryParameter("key-marker", KeyMarker);
        requestMessageBuilder.AddQueryParameter("upload-id-marker", UploadIdMarker);
        requestMessageBuilder.AddQueryParameter("max-uploads", MAX_UPLOAD_COUNT.ToString());
        return requestMessageBuilder;
    }
}

public class PresignedGetObjectArgs : ObjectArgs<PresignedGetObjectArgs>
{
    public PresignedGetObjectArgs()
    {
        RequestMethod = HttpMethod.Get;
    }

    internal int Expiry { get; set; }
    internal DateTime? RequestDate { get; set; }

    internal override void Validate()
    {
        base.Validate();
        if (!utils.IsValidExpiry(Expiry))
            throw new InvalidExpiryRangeException("expiry range should be between 1 and " +
                                                  Constants.DefaultExpiryTime);
    }

    public PresignedGetObjectArgs WithExpiry(int expiry)
    {
        Expiry = expiry;
        return this;
    }

    public PresignedGetObjectArgs WithRequestDate(DateTime? d)
    {
        RequestDate = d;
        return this;
    }
}

public class StatObjectArgs : ObjectConditionalQueryArgs<StatObjectArgs>
{
    public StatObjectArgs()
    {
        RequestMethod = HttpMethod.Head;
    }

    internal long ObjectOffset { get; private set; }
    internal long ObjectLength { get; private set; }
    internal bool OffsetLengthSet { get; set; }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (!string.IsNullOrEmpty(VersionId))
            requestMessageBuilder.AddQueryParameter("versionId", $"{VersionId}");
        if (Headers.ContainsKey(S3ZipExtractKey))
            requestMessageBuilder.AddQueryParameter(S3ZipExtractKey, Headers[S3ZipExtractKey]);

        return requestMessageBuilder;
    }

    internal override void Validate()
    {
        base.Validate();
        if (!string.IsNullOrEmpty(NotMatchETag) && !string.IsNullOrEmpty(MatchETag))
            throw new InvalidOperationException("Invalid to set both Etag match conditions " + nameof(NotMatchETag) +
                                                " and " + nameof(MatchETag));
        if (!ModifiedSince.Equals(default) &&
            !UnModifiedSince.Equals(default))
            throw new InvalidOperationException("Invalid to set both modified date match conditions " +
                                                nameof(ModifiedSince) + " and " + nameof(UnModifiedSince));
        if (OffsetLengthSet)
        {
            if (ObjectOffset < 0 || ObjectLength < 0)
                throw new ArgumentException(nameof(ObjectOffset) + " and " + nameof(ObjectLength) +
                                            "cannot be less than 0.");
            if (ObjectOffset == 0 && ObjectLength == 0)
                throw new ArgumentException("Either " + nameof(ObjectOffset) + " or " + nameof(ObjectLength) +
                                            " must be greater than 0.");
        }

        Populate();
    }

    private void Populate()
    {
        Headers = Headers ?? new Dictionary<string, string>();
        if (SSE != null && SSE.GetType().Equals(EncryptionType.SSE_C)) SSE.Marshal(Headers);
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

    public StatObjectArgs WithOffsetAndLength(long offset, long length)
    {
        OffsetLengthSet = true;
        ObjectOffset = offset < 0 ? 0 : offset;
        ObjectLength = length < 0 ? 0 : length;
        return this;
    }

    public StatObjectArgs WithLength(long length)
    {
        OffsetLengthSet = true;
        ObjectOffset = 0;
        ObjectLength = length < 0 ? 0 : length;
        return this;
    }
}

public class PresignedPostPolicyArgs : ObjectArgs<PresignedPostPolicyArgs>
{
    internal PostPolicy Policy { get; set; }
    internal DateTime Expiration { get; set; }

    internal string Region { get; set; }

    protected new void Validate()
    {
        var checkPolicy = false;
        try
        {
            utils.ValidateBucketName(BucketName);
            utils.ValidateObjectName(ObjectName);
        }
        catch (Exception ex)
        {
            if (ex is InvalidBucketNameException || ex is InvalidObjectNameException)
                checkPolicy = true;
            else
                throw;
        }

        if (checkPolicy)
        {
            if (!Policy.IsBucketSet())
                throw new InvalidOperationException("For the " + nameof(Policy) + " bucket should be set");

            if (!Policy.IsKeySet())
                throw new InvalidOperationException("For the " + nameof(Policy) + " key should be set");

            if (!Policy.IsExpirationSet())
                throw new InvalidOperationException("For the " + nameof(Policy) + " expiration should be set");
            BucketName = Policy.Bucket;
            ObjectName = Policy.Key;
        }

        if (string.IsNullOrEmpty(Expiration.ToString()))
            throw new InvalidOperationException("For the " + nameof(Policy) + " expiration should be set");
    }

    public PresignedPostPolicyArgs WithExpiration(DateTime ex)
    {
        Expiration = ex;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        return requestMessageBuilder;
    }

    internal PresignedPostPolicyArgs WithRegion(string region)
    {
        Region = region;
        return this;
    }

    internal PresignedPostPolicyArgs WithSessionToken(string sessionToken)
    {
        Policy.SetSessionToken(sessionToken);
        return this;
    }

    internal PresignedPostPolicyArgs WithDate(DateTime date)
    {
        Policy.SetDate(date);
        return this;
    }

    internal PresignedPostPolicyArgs WithCredential(string credential)
    {
        Policy.SetCredential(credential);
        return this;
    }

    internal PresignedPostPolicyArgs WithAlgorithm(string algorithm)
    {
        Policy.SetAlgorithm(algorithm);
        return this;
    }

    internal PresignedPostPolicyArgs WithSignature(string signature)
    {
        Policy.SetSignature(signature);
        return this;
    }

    public PresignedPostPolicyArgs WithPolicy(PostPolicy policy)
    {
        Policy = policy;
        if (policy.expiration != DateTime.MinValue)
            // policy.expiration has an assigned value
            Expiration = policy.expiration;
        return this;
    }
}

public class PresignedPutObjectArgs : ObjectArgs<PresignedPutObjectArgs>
{
    public PresignedPutObjectArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal int Expiry { get; set; }

    protected new void Validate()
    {
        base.Validate();
        if (!utils.IsValidExpiry(Expiry))
            throw new InvalidExpiryRangeException("Expiry range should be between 1 seconds and " +
                                                  Constants.DefaultExpiryTime + " seconds");
    }

    public PresignedPutObjectArgs WithExpiry(int ex)
    {
        Expiry = ex;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        return requestMessageBuilder;
    }
}

public class RemoveUploadArgs : EncryptionArgs<RemoveUploadArgs>
{
    public RemoveUploadArgs()
    {
        RequestMethod = HttpMethod.Delete;
    }

    internal string UploadId { get; private set; }

    public RemoveUploadArgs WithUploadId(string id)
    {
        UploadId = id;
        return this;
    }

    internal override void Validate()
    {
        base.Validate();
        if (string.IsNullOrEmpty(UploadId))
            throw new InvalidOperationException(nameof(UploadId) +
                                                " cannot be empty. Please assign a valid upload ID to remove.");
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploadId", $"{UploadId}");
        return requestMessageBuilder;
    }
}

public class RemoveIncompleteUploadArgs : EncryptionArgs<RemoveIncompleteUploadArgs>
{
    public RemoveIncompleteUploadArgs()
    {
        RequestMethod = HttpMethod.Delete;
    }
}

public class GetObjectLegalHoldArgs : ObjectVersionArgs<GetObjectLegalHoldArgs>
{
    public GetObjectLegalHoldArgs()
    {
        RequestMethod = HttpMethod.Get;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("legal-hold", "");
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);
        return requestMessageBuilder;
    }
}

public class SetObjectLegalHoldArgs : ObjectVersionArgs<SetObjectLegalHoldArgs>
{
    public SetObjectLegalHoldArgs()
    {
        RequestMethod = HttpMethod.Put;
        LegalHoldON = false;
    }

    internal bool LegalHoldON { get; private set; }

    public SetObjectLegalHoldArgs WithLegalHold(bool status)
    {
        LegalHoldON = status;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("legal-hold", "");
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);
        var config = new ObjectLegalHoldConfiguration(LegalHoldON);
        var body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
        requestMessageBuilder.AddXmlBody(body);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            utils.getMD5SumStr(Encoding.UTF8.GetBytes(body)));
        return requestMessageBuilder;
    }
}

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
        if (CallBack == null && FuncCallBack == null && string.IsNullOrEmpty(FileName))
            throw new MinioException("Atleast one of " + nameof(CallBack) + ", CallBack method or " + nameof(FileName) +
                                     " file path to save need to be set for GetObject operation.");
        if (OffsetLengthSet)
        {
            if (ObjectOffset < 0) throw new ArgumentException("Offset should be zero or greater", nameof(ObjectOffset));

            if (ObjectLength < 0)
                throw new ArgumentException("Length should be greater than or equal to zero", nameof(ObjectLength));
        }

        if (FileName != null) utils.ValidateFile(FileName);
        Populate();
    }

    private void Populate()
    {
        Headers = Headers ?? new Dictionary<string, string>();
        if (SSE != null && SSE.GetType().Equals(EncryptionType.SSE_C)) SSE.Marshal(Headers);

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

        if (Headers.ContainsKey(S3ZipExtractKey))
            requestMessageBuilder.AddQueryParameter(S3ZipExtractKey, Headers[S3ZipExtractKey]);

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

public class RemoveObjectArgs : ObjectArgs<RemoveObjectArgs>
{
    public RemoveObjectArgs()
    {
        RequestMethod = HttpMethod.Delete;
        BypassGovernanceMode = null;
    }

    internal string VersionId { get; private set; }
    internal bool? BypassGovernanceMode { get; private set; }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (!string.IsNullOrEmpty(VersionId))
        {
            requestMessageBuilder.AddQueryParameter("versionId", $"{VersionId}");
            if (BypassGovernanceMode != null && BypassGovernanceMode.Value)
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-bypass-governance-retention",
                    BypassGovernanceMode.Value.ToString());
        }

        return requestMessageBuilder;
    }

    public RemoveObjectArgs WithVersionId(string ver)
    {
        VersionId = ver;
        return this;
    }

    public RemoveObjectArgs WithBypassGovernanceMode(bool? mode)
    {
        BypassGovernanceMode = mode;
        return this;
    }
}

public class RemoveObjectsArgs : ObjectArgs<RemoveObjectsArgs>
{
    public RemoveObjectsArgs()
    {
        ObjectName = null;
        ObjectNames = new List<string>();
        ObjectNamesVersions = new List<Tuple<string, string>>();
        RequestMethod = HttpMethod.Post;
    }

    internal List<string> ObjectNames { get; private set; }

    // Each element in the list is a Tuple. Each Tuple has an Object name & the version ID.
    internal List<Tuple<string, string>> ObjectNamesVersions { get; }

    public RemoveObjectsArgs WithObjectAndVersions(string objectName, List<string> versions)
    {
        foreach (var vid in versions) ObjectNamesVersions.Add(new Tuple<string, string>(objectName, vid));
        return this;
    }

    // Tuple<string, List<string>>. Tuple object name -> List of Version IDs.
    public RemoveObjectsArgs WithObjectsVersions(List<Tuple<string, List<string>>> objectsVersionsList)
    {
        foreach (var objVersions in objectsVersionsList)
        foreach (var vid in objVersions.Item2)
            ObjectNamesVersions.Add(new Tuple<string, string>(objVersions.Item1, vid));
        return this;
    }

    public RemoveObjectsArgs WithObjectsVersions(List<Tuple<string, string>> objectVersions)
    {
        ObjectNamesVersions.AddRange(objectVersions);
        return this;
    }

    public RemoveObjectsArgs WithObjects(List<string> names)
    {
        ObjectNames = names;
        return this;
    }

    internal override void Validate()
    {
        // Skip object name validation.
        utils.ValidateBucketName(BucketName);
        if (!string.IsNullOrEmpty(ObjectName))
            throw new InvalidOperationException(nameof(ObjectName) + " is set. Please use " + nameof(WithObjects) +
                                                "or " +
                                                nameof(WithObjectsVersions) + " method to set objects to be deleted.");
        if ((ObjectNames == null && ObjectNamesVersions == null) ||
            (ObjectNames.Count == 0 && ObjectNamesVersions.Count == 0))
            throw new InvalidOperationException(
                "Please assign list of object names or object names and version IDs to remove using method(s) " +
                nameof(WithObjects) + " " + nameof(WithObjectsVersions));
    }


    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        XElement deleteObjectsRequest = null;
        var objects = new List<XElement>();
        requestMessageBuilder.AddQueryParameter("delete", "");
        if (ObjectNamesVersions.Count > 0)
        {
            // Object(s) & multiple versions
            foreach (var objTuple in ObjectNamesVersions)
                objects.Add(new XElement("Object",
                    new XElement("Key", objTuple.Item1),
                    new XElement("VersionId", objTuple.Item2)));
            deleteObjectsRequest = new XElement("Delete", objects,
                new XElement("Quiet", true));
            requestMessageBuilder.AddXmlBody(Convert.ToString(deleteObjectsRequest));
        }
        else
        {
            // Multiple Objects
            foreach (var obj in ObjectNames)
                objects.Add(new XElement("Object",
                    new XElement("Key", obj)));
            deleteObjectsRequest = new XElement("Delete", objects,
                new XElement("Quiet", true));
            requestMessageBuilder.AddXmlBody(Convert.ToString(deleteObjectsRequest));
        }

        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            utils.getMD5SumStr(Encoding.UTF8.GetBytes(Convert.ToString(deleteObjectsRequest))));

        return requestMessageBuilder;
    }
}

public class SetObjectTagsArgs : ObjectVersionArgs<SetObjectTagsArgs>
{
    public SetObjectTagsArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal Tagging ObjectTags { get; private set; }


    public SetObjectTagsArgs WithTagging(Tagging tags)
    {
        ObjectTags = Tagging.GetObjectTags(tags.GetTags());
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("tagging", "");
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);
        var body = ObjectTags.MarshalXML();
        requestMessageBuilder.AddXmlBody(body);

        return requestMessageBuilder;
    }

    internal override void Validate()
    {
        base.Validate();
        if (ObjectTags == null || ObjectTags.GetTags().Count == 0)
            throw new InvalidOperationException("Unable to set empty tags.");
    }
}

public class GetObjectTagsArgs : ObjectVersionArgs<GetObjectTagsArgs>
{
    public GetObjectTagsArgs()
    {
        RequestMethod = HttpMethod.Get;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("tagging", "");
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);
        return requestMessageBuilder;
    }
}

public class RemoveObjectTagsArgs : ObjectVersionArgs<RemoveObjectTagsArgs>
{
    public RemoveObjectTagsArgs()
    {
        RequestMethod = HttpMethod.Delete;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("tagging", "");
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);
        return requestMessageBuilder;
    }
}

public class SetObjectRetentionArgs : ObjectVersionArgs<SetObjectRetentionArgs>
{
    public SetObjectRetentionArgs()
    {
        RequestMethod = HttpMethod.Put;
        RetentionUntilDate = default;
        Mode = RetentionMode.GOVERNANCE;
    }

    internal bool BypassGovernanceMode { get; set; }
    internal RetentionMode Mode { get; set; }
    internal DateTime RetentionUntilDate { get; set; }

    internal override void Validate()
    {
        base.Validate();
        if (RetentionUntilDate.Equals(default))
            throw new InvalidOperationException("Retention Period is not set. Please set using " +
                                                nameof(WithRetentionUntilDate) + ".");
        if (DateTime.Compare(RetentionUntilDate, DateTime.Now) <= 0)
            throw new InvalidOperationException("Retention until date set using " + nameof(WithRetentionUntilDate) +
                                                " needs to be in the future.");
    }

    public SetObjectRetentionArgs WithBypassGovernanceMode(bool bypass = true)
    {
        BypassGovernanceMode = bypass;
        return this;
    }

    public SetObjectRetentionArgs WithRetentionMode(RetentionMode mode)
    {
        Mode = mode;
        return this;
    }

    public SetObjectRetentionArgs WithRetentionUntilDate(DateTime date)
    {
        RetentionUntilDate = date;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("retention", "");
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);
        if (BypassGovernanceMode)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-bypass-governance-retention", "true");
        var config = new ObjectRetentionConfiguration(RetentionUntilDate, Mode);
        var body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
        requestMessageBuilder.AddXmlBody(body);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            utils.getMD5SumStr(Encoding.UTF8.GetBytes(body)));
        return requestMessageBuilder;
    }
}

public class GetObjectRetentionArgs : ObjectVersionArgs<GetObjectRetentionArgs>
{
    public GetObjectRetentionArgs()
    {
        RequestMethod = HttpMethod.Get;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("retention", "");
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);
        return requestMessageBuilder;
    }
}

public class ClearObjectRetentionArgs : ObjectVersionArgs<ClearObjectRetentionArgs>
{
    public ClearObjectRetentionArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    public static string EmptyRetentionConfigXML()
    {
        var sw = new StringWriter(CultureInfo.InvariantCulture);
        var settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.Indent = true;
        var xw = XmlWriter.Create(sw, settings);
        xw.WriteStartElement("Retention");
        xw.WriteString("");
        xw.WriteFullEndElement();
        xw.Flush();
        return sw.ToString();
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("retention", "");
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);
        // Required for Clear Object Retention.
        requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-bypass-governance-retention", "true");
        var body = EmptyRetentionConfigXML();
        requestMessageBuilder.AddXmlBody(body);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            utils.getMD5SumStr(Encoding.UTF8.GetBytes(body)));
        return requestMessageBuilder;
    }
}

public class CopySourceObjectArgs : ObjectConditionalQueryArgs<CopySourceObjectArgs>
{
    public CopySourceObjectArgs()
    {
        RequestMethod = HttpMethod.Put;
        CopyOperationConditions = new CopyConditions();
        Headers = new Dictionary<string, string>();
    }

    internal string CopySourceObjectPath { get; set; }
    internal CopyConditions CopyOperationConditions { get; set; }

    internal override void Validate()
    {
        base.Validate();
    }

    public CopySourceObjectArgs WithCopyConditions(CopyConditions cp)
    {
        CopyOperationConditions = cp != null ? cp.Clone() : new CopyConditions();
        return this;
    }

    // internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    // {
    //     return requestMessageBuilder;
    // }
}

internal class CopyObjectRequestArgs : ObjectWriteArgs<CopyObjectRequestArgs>
{
    internal CopyObjectRequestArgs()
    {
        RequestMethod = HttpMethod.Put;
        Headers = new Dictionary<string, string>();
        CopyOperationObjectType = typeof(CopyObjectResult);
    }

    internal CopySourceObjectArgs SourceObject { get; set; }
    internal ObjectStat SourceObjectInfo { get; set; }
    internal Type CopyOperationObjectType { get; set; }
    internal bool ReplaceTagsDirective { get; set; }
    internal bool ReplaceMetadataDirective { get; set; }
    internal string StorageClass { get; set; }
    internal Dictionary<string, string> QueryMap { get; set; }
    internal CopyConditions CopyCondition { get; set; }
    internal RetentionMode ObjectLockRetentionMode { get; set; }
    internal DateTime RetentionUntilDate { get; set; }
    internal bool ObjectLockSet { get; set; }

    internal CopyObjectRequestArgs WithQueryMap(Dictionary<string, string> queryMap)
    {
        QueryMap = new Dictionary<string, string>(queryMap);
        return this;
    }

    internal CopyObjectRequestArgs WithPartCondition(CopyConditions partCondition)
    {
        CopyCondition = partCondition.Clone();
        Headers = Headers ?? new Dictionary<string, string>();
        Headers["x-amz-copy-source-range"] = "bytes=" + partCondition.byteRangeStart + "-" + partCondition.byteRangeEnd;

        return this;
    }

    internal CopyObjectRequestArgs WithReplaceMetadataDirective(bool replace)
    {
        ReplaceMetadataDirective = replace;
        return this;
    }

    internal CopyObjectRequestArgs WithReplaceTagsDirective(bool replace)
    {
        ReplaceTagsDirective = replace;
        return this;
    }

    public CopyObjectRequestArgs WithCopyObjectSource(CopySourceObjectArgs cs)
    {
        if (cs == null)
            throw new InvalidOperationException("The copy source object needed for copy operation is not initialized.");

        SourceObject = SourceObject ?? new CopySourceObjectArgs();
        SourceObject.RequestMethod = HttpMethod.Put;
        SourceObject.BucketName = cs.BucketName;
        SourceObject.ObjectName = cs.ObjectName;
        SourceObject.VersionId = cs.VersionId;
        SourceObject.SSE = cs.SSE;
        SourceObject.Headers = new Dictionary<string, string>(cs.Headers);
        SourceObject.MatchETag = cs.MatchETag;
        SourceObject.ModifiedSince = cs.ModifiedSince;
        SourceObject.NotMatchETag = cs.NotMatchETag;
        SourceObject.UnModifiedSince = cs.UnModifiedSince;
        SourceObject.CopySourceObjectPath = $"{cs.BucketName}/{utils.UrlEncode(cs.ObjectName)}";
        SourceObject.CopyOperationConditions = cs.CopyOperationConditions?.Clone();
        return this;
    }

    public CopyObjectRequestArgs WithSourceObjectInfo(ObjectStat stat)
    {
        SourceObjectInfo = stat;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        var sourceObjectPath = SourceObject.BucketName + "/" + utils.UrlEncode(SourceObject.ObjectName);
        if (!string.IsNullOrEmpty(SourceObject.VersionId)) sourceObjectPath += "?versionId=" + SourceObject.VersionId;
        // Set the object source
        requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source", sourceObjectPath);

        if (QueryMap != null)
            foreach (var query in QueryMap)
                requestMessageBuilder.AddQueryParameter(query.Key, query.Value);

        if (SourceObject.CopyOperationConditions != null)
            foreach (var item in SourceObject.CopyOperationConditions.GetConditions())
                requestMessageBuilder.AddOrUpdateHeaderParameter(item.Key, item.Value);
        if (!string.IsNullOrEmpty(MatchETag))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-match", MatchETag);
        if (!string.IsNullOrEmpty(NotMatchETag))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-none-match", NotMatchETag);
        if (ModifiedSince != default)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-unmodified-since",
                utils.To8601String(ModifiedSince));
        if (UnModifiedSince != default)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-modified-since",
                utils.To8601String(UnModifiedSince));
        if (ObjectTags != null && ObjectTags.TaggingSet != null
                               && ObjectTags.TaggingSet.Tag.Count > 0)
        {
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging", ObjectTags.GetTagString());
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging-directive",
                ReplaceTagsDirective ? "REPLACE" : "COPY");
            if (ReplaceMetadataDirective)
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging-directive", "REPLACE");
        }

        if (ReplaceMetadataDirective)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-metadata-directive", "REPLACE");
        if (!string.IsNullOrEmpty(StorageClass))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-storage-class", StorageClass);
        if (ObjectLockSet)
        {
            if (!RetentionUntilDate.Equals(default))
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                    utils.To8601String(RetentionUntilDate));
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode",
                ObjectLockRetentionMode == RetentionMode.GOVERNANCE ? "GOVERNANCE" : "COMPLIANCE");
        }

        if (RequestBody != null) requestMessageBuilder.SetBody(RequestBody);
        return requestMessageBuilder;
    }

    internal CopyObjectRequestArgs WithCopyOperationObjectType(Type cp)
    {
        CopyOperationObjectType = cp;
        return this;
    }

    public CopyObjectRequestArgs WithObjectLockMode(RetentionMode mode)
    {
        ObjectLockSet = true;
        ObjectLockRetentionMode = mode;
        return this;
    }

    public CopyObjectRequestArgs WithObjectLockRetentionDate(DateTime untilDate)
    {
        ObjectLockSet = true;
        RetentionUntilDate = new DateTime(untilDate.Year, untilDate.Month, untilDate.Day,
            untilDate.Hour, untilDate.Minute, untilDate.Second);
        return this;
    }

    internal override void Validate()
    {
        utils.ValidateBucketName(BucketName); //Object name can be same as that of source.
        if (SourceObject == null) throw new InvalidOperationException(nameof(SourceObject) + " has not been assigned.");
        Populate();
    }

    internal void Populate()
    {
        ObjectName = string.IsNullOrEmpty(ObjectName) ? SourceObject.ObjectName : ObjectName;
        // Opting for concat as Headers may have byte range info .etc.
        if (!ReplaceMetadataDirective && SourceObjectInfo.MetaData != null)
            Headers = SourceObjectInfo.MetaData.Concat(Headers).GroupBy(item => item.Key)
                .ToDictionary(item => item.Key, item => item.First().Value);
        else if (ReplaceMetadataDirective) Headers = Headers ?? new Dictionary<string, string>();
    }
}

public class CopyObjectArgs : ObjectWriteArgs<CopyObjectArgs>
{
    public CopyObjectArgs()
    {
        RequestMethod = HttpMethod.Put;
        SourceObject = new CopySourceObjectArgs();
        ReplaceTagsDirective = false;
        ReplaceMetadataDirective = false;
        ObjectLockSet = false;
        RetentionUntilDate = default;
    }

    internal CopySourceObjectArgs SourceObject { get; set; }
    internal ObjectStat SourceObjectInfo { get; set; }
    internal bool ReplaceTagsDirective { get; set; }
    internal bool ReplaceMetadataDirective { get; set; }
    internal string StorageClass { get; set; }
    internal RetentionMode ObjectLockRetentionMode { get; set; }
    internal DateTime RetentionUntilDate { get; set; }
    internal bool ObjectLockSet { get; set; }

    internal override void Validate()
    {
        utils.ValidateBucketName(BucketName);
        if (SourceObject == null)
            throw new InvalidOperationException(nameof(SourceObject) + " has not been assigned. Please use " +
                                                nameof(WithCopyObjectSource));
        if (SourceObjectInfo == null)
            throw new InvalidOperationException(
                "StatObject result for the copy source object needed to continue copy operation. Use " +
                nameof(WithCopyObjectSourceStats) + " to initialize StatObject result.");
        if (!string.IsNullOrEmpty(NotMatchETag) && !string.IsNullOrEmpty(MatchETag))
            throw new InvalidOperationException("Invalid to set both Etag match conditions " + nameof(NotMatchETag) +
                                                " and " + nameof(MatchETag));
        if (!ModifiedSince.Equals(default) &&
            !UnModifiedSince.Equals(default))
            throw new InvalidOperationException("Invalid to set both modified date match conditions " +
                                                nameof(ModifiedSince) + " and " + nameof(UnModifiedSince));
        Populate();
    }

    private void Populate()
    {
        if (string.IsNullOrEmpty(ObjectName)) ObjectName = SourceObject.ObjectName;
        if (SSE != null && SSE.GetType().Equals(EncryptionType.SSE_C))
        {
            Headers = new Dictionary<string, string>();
            SSE.Marshal(Headers);
        }

        if (!ReplaceMetadataDirective)
        {
            // Check in copy conditions if replace metadata has been set
            var copyReplaceMeta = SourceObject.CopyOperationConditions != null
                ? SourceObject.CopyOperationConditions.HasReplaceMetadataDirective()
                : false;
            WithReplaceMetadataDirective(copyReplaceMeta);
        }

        Headers = Headers ?? new Dictionary<string, string>();
        if (ReplaceMetadataDirective)
        {
            if (Headers != null)
                foreach (var pair in SourceObjectInfo.MetaData)
                {
                    var comparer = StringComparer.OrdinalIgnoreCase;
                    var newDictionary = new Dictionary<string, string>(Headers, comparer);

                    if (newDictionary.ContainsKey(pair.Key)) SourceObjectInfo.MetaData.Remove(pair.Key);
                }

            Headers = Headers
                .Concat(SourceObjectInfo.MetaData)
                .GroupBy(item => item.Key)
                .ToDictionary(item => item.Key, item =>
                    item.Last().Value);
        }

        if (Headers != null)
        {
            var newKVList = new List<Tuple<string, string>>();
            foreach (var item in Headers)
            {
                var key = item.Key;
                if (!OperationsUtil.IsSupportedHeader(item.Key) &&
                    !item.Key.StartsWith("x-amz-meta",
                        StringComparison.OrdinalIgnoreCase) &&
                    !OperationsUtil.IsSSEHeader(key))
                {
                    newKVList.Add(new Tuple<string, string>("x-amz-meta-" +
                                                            key.ToLowerInvariant(), item.Value));
                    Headers.Remove(item.Key);
                }

                newKVList.Add(new Tuple<string, string>(key, item.Value));
            }

            foreach (var item in newKVList) Headers[item.Item1] = item.Item2;
        }
    }

    public CopyObjectArgs WithCopyObjectSource(CopySourceObjectArgs cs)
    {
        if (cs == null)
            throw new InvalidOperationException("The copy source object needed for copy operation is not initialized.");

        SourceObject.RequestMethod = HttpMethod.Put;
        SourceObject = SourceObject ?? new CopySourceObjectArgs();
        SourceObject.BucketName = cs.BucketName;
        SourceObject.ObjectName = cs.ObjectName;
        SourceObject.VersionId = cs.VersionId;
        SourceObject.SSE = cs.SSE;
        SourceObject.Headers = cs.Headers;
        SourceObject.MatchETag = cs.MatchETag;
        SourceObject.ModifiedSince = cs.ModifiedSince;
        SourceObject.NotMatchETag = cs.NotMatchETag;
        SourceObject.UnModifiedSince = cs.UnModifiedSince;
        SourceObject.CopySourceObjectPath = $"{cs.BucketName}/{utils.UrlEncode(cs.ObjectName)}";
        SourceObject.CopyOperationConditions = cs.CopyOperationConditions?.Clone();
        return this;
    }

    public CopyObjectArgs WithReplaceTagsDirective(bool replace)
    {
        ReplaceTagsDirective = replace;
        return this;
    }

    public CopyObjectArgs WithReplaceMetadataDirective(bool replace)
    {
        ReplaceMetadataDirective = replace;
        return this;
    }

    public CopyObjectArgs WithObjectLockMode(RetentionMode mode)
    {
        ObjectLockSet = true;
        ObjectLockRetentionMode = mode;
        return this;
    }

    public CopyObjectArgs WithObjectLockRetentionDate(DateTime untilDate)
    {
        ObjectLockSet = true;
        RetentionUntilDate = new DateTime(untilDate.Year, untilDate.Month, untilDate.Day,
            untilDate.Hour, untilDate.Minute, untilDate.Second);
        return this;
    }

    internal CopyObjectArgs WithCopyObjectSourceStats(ObjectStat info)
    {
        SourceObjectInfo = info;
        if (info.MetaData != null && !ReplaceMetadataDirective)
        {
            SourceObject.Headers = SourceObject.Headers ?? new Dictionary<string, string>();
            SourceObject.Headers = SourceObject.Headers.Concat(info.MetaData).GroupBy(item => item.Key)
                .ToDictionary(item => item.Key, item => item.First().Value);
        }

        return this;
    }

    internal CopyObjectArgs WithStorageClass(string storageClass)
    {
        StorageClass = storageClass;
        return this;
    }

    public CopyObjectArgs WithRetentionUntilDate(DateTime date)
    {
        ObjectLockSet = true;
        RetentionUntilDate = date;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (!string.IsNullOrEmpty(MatchETag))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-match", MatchETag);
        if (!string.IsNullOrEmpty(NotMatchETag))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-none-match", NotMatchETag);
        if (ModifiedSince != default)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-unmodified-since",
                utils.To8601String(ModifiedSince));
        if (UnModifiedSince != default)
            requestMessageBuilder.Request.Headers.Add("x-amz-copy-source-if-modified-since",
                utils.To8601String(UnModifiedSince));
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);
        if (ObjectTags != null && ObjectTags.TaggingSet != null
                               && ObjectTags.TaggingSet.Tag.Count > 0)
        {
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging", ObjectTags.GetTagString());
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging-directive",
                ReplaceTagsDirective ? "COPY" : "REPLACE");
        }

        if (ReplaceMetadataDirective)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-metadata-directive", "REPLACE");
        if (!string.IsNullOrEmpty(StorageClass))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-storage-class", StorageClass);
        if (LegalHoldEnabled != null && LegalHoldEnabled.Value)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-legal-hold", "ON");
        if (ObjectLockSet)
        {
            if (!RetentionUntilDate.Equals(default))
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                    utils.To8601String(RetentionUntilDate));
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode",
                ObjectLockRetentionMode == RetentionMode.GOVERNANCE ? "GOVERNANCE" : "COMPLIANCE");
        }

        return requestMessageBuilder;
    }
}

internal class NewMultipartUploadArgs<T> : ObjectWriteArgs<T>
    where T : NewMultipartUploadArgs<T>
{
    internal NewMultipartUploadArgs()
    {
        RequestMethod = HttpMethod.Post;
    }

    internal RetentionMode ObjectLockRetentionMode { get; set; }
    internal DateTime RetentionUntilDate { get; set; }
    internal bool ObjectLockSet { get; set; }

    public NewMultipartUploadArgs<T> WithObjectLockMode(RetentionMode mode)
    {
        ObjectLockSet = true;
        ObjectLockRetentionMode = mode;
        return this;
    }

    public NewMultipartUploadArgs<T> WithObjectLockRetentionDate(DateTime untilDate)
    {
        ObjectLockSet = true;
        RetentionUntilDate = new DateTime(untilDate.Year, untilDate.Month, untilDate.Day,
            untilDate.Hour, untilDate.Minute, untilDate.Second);
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploads", "");
        if (ObjectLockSet)
        {
            if (!RetentionUntilDate.Equals(default))
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                    utils.To8601String(RetentionUntilDate));
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode",
                ObjectLockRetentionMode == RetentionMode.GOVERNANCE ? "GOVERNANCE" : "COMPLIANCE");
        }

        requestMessageBuilder.AddOrUpdateHeaderParameter("content-type", ContentType);

        return requestMessageBuilder;
    }
}

internal class NewMultipartUploadPutArgs : NewMultipartUploadArgs<NewMultipartUploadPutArgs>
{
    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploads", "");

        if (ObjectTags != null && ObjectTags.TaggingSet != null
                               && ObjectTags.TaggingSet.Tag.Count > 0)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging", ObjectTags.GetTagString());

        requestMessageBuilder.AddOrUpdateHeaderParameter("content-type", ContentType);

        return requestMessageBuilder;
    }
}

internal class MultipartCopyUploadArgs : ObjectWriteArgs<MultipartCopyUploadArgs>
{
    internal MultipartCopyUploadArgs(CopyObjectArgs args)
    {
        if (args == null || args.SourceObject == null)
        {
            var message = args == null
                ? "The constructor of " + nameof(CopyObjectRequestArgs) +
                  "initialized with arguments of CopyObjectArgs null."
                : "The constructor of " + nameof(CopyObjectRequestArgs) +
                  "initialized with arguments of CopyObjectArgs type but with " + nameof(args.SourceObject) +
                  " not initialized.";
            throw new InvalidOperationException(message);
        }

        RequestMethod = HttpMethod.Put;

        SourceObject = new CopySourceObjectArgs();
        SourceObject.BucketName = args.SourceObject.BucketName;
        SourceObject.ObjectName = args.SourceObject.ObjectName;
        SourceObject.VersionId = args.SourceObject.VersionId;
        SourceObject.CopyOperationConditions = args.SourceObject.CopyOperationConditions.Clone();
        SourceObject.MatchETag = args.SourceObject.MatchETag;
        SourceObject.ModifiedSince = args.SourceObject.ModifiedSince;
        SourceObject.NotMatchETag = args.SourceObject.NotMatchETag;
        SourceObject.UnModifiedSince = args.SourceObject.UnModifiedSince;

        // Destination part.
        BucketName = args.BucketName;
        ObjectName = args.ObjectName ?? args.SourceObject.ObjectName;
        SSE = args.SSE;
        SSE?.Marshal(Headers);
        VersionId = args.VersionId;
        SourceObjectInfo = args.SourceObjectInfo;
        // Header part
        if (!args.ReplaceMetadataDirective)
            Headers = new Dictionary<string, string>(args.SourceObjectInfo.MetaData);
        else if (args.ReplaceMetadataDirective) Headers = Headers ?? new Dictionary<string, string>();
        if (Headers != null)
        {
            var newKVList = new List<Tuple<string, string>>();
            foreach (var item in Headers)
            {
                var key = item.Key;
                if (!OperationsUtil.IsSupportedHeader(item.Key) &&
                    !item.Key.StartsWith("x-amz-meta", StringComparison.OrdinalIgnoreCase) &&
                    !OperationsUtil.IsSSEHeader(key))
                    newKVList.Add(new Tuple<string, string>("x-amz-meta-" + key.ToLowerInvariant(), item.Value));
            }

            foreach (var item in newKVList) Headers[item.Item1] = item.Item2;
        }

        ReplaceTagsDirective = args.ReplaceTagsDirective;
        if (args.ReplaceTagsDirective && args.ObjectTags != null &&
            args.ObjectTags.TaggingSet.Tag.Count > 0) // Tags of Source object
            ObjectTags = Tagging.GetObjectTags(args.ObjectTags.GetTags());
    }

    internal MultipartCopyUploadArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal CopySourceObjectArgs SourceObject { get; set; }
    internal ObjectStat SourceObjectInfo { get; set; }
    internal long CopySize { get; set; }
    internal bool ReplaceMetadataDirective { get; set; }
    internal bool ReplaceTagsDirective { get; set; }
    internal string StorageClass { get; set; }
    internal RetentionMode ObjectLockRetentionMode { get; set; }
    internal DateTime RetentionUntilDate { get; set; }
    internal bool ObjectLockSet { get; set; }

    internal MultipartCopyUploadArgs WithCopySize(long copySize)
    {
        CopySize = copySize;
        return this;
    }

    internal MultipartCopyUploadArgs WithStorageClass(string storageClass)
    {
        StorageClass = storageClass;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (ObjectTags != null && ObjectTags.TaggingSet != null
                               && ObjectTags.TaggingSet.Tag.Count > 0)
        {
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging", ObjectTags.GetTagString());
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging-directive",
                ReplaceTagsDirective ? "REPLACE" : "COPY");
        }

        if (ReplaceMetadataDirective)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-metadata-directive", "REPLACE");
        if (!string.IsNullOrEmpty(StorageClass))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-storage-class", StorageClass);
        if (ObjectLockSet)
        {
            if (!RetentionUntilDate.Equals(default))
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                    utils.To8601String(RetentionUntilDate));
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode",
                ObjectLockRetentionMode == RetentionMode.GOVERNANCE ? "GOVERNANCE" : "COMPLIANCE");
        }

        return requestMessageBuilder;
    }

    internal MultipartCopyUploadArgs WithReplaceMetadataDirective(bool replace)
    {
        ReplaceMetadataDirective = replace;
        return this;
    }

    internal MultipartCopyUploadArgs WithObjectLockMode(RetentionMode mode)
    {
        ObjectLockSet = true;
        ObjectLockRetentionMode = mode;
        return this;
    }

    internal MultipartCopyUploadArgs WithObjectLockRetentionDate(DateTime untilDate)
    {
        ObjectLockSet = true;
        RetentionUntilDate = new DateTime(untilDate.Year, untilDate.Month, untilDate.Day,
            untilDate.Hour, untilDate.Minute, untilDate.Second);
        return this;
    }
}

internal class NewMultipartUploadCopyArgs : NewMultipartUploadArgs<NewMultipartUploadCopyArgs>
{
    internal bool ReplaceMetadataDirective { get; set; }
    internal bool ReplaceTagsDirective { get; set; }
    internal string StorageClass { get; set; }
    internal ObjectStat SourceObjectInfo { get; set; }
    internal CopySourceObjectArgs SourceObject { get; set; }

    internal override void Validate()
    {
        base.Validate();
        if (SourceObjectInfo == null || SourceObject == null)
            throw new InvalidOperationException(nameof(SourceObjectInfo) + " and " + nameof(SourceObject) +
                                                " need to be initialized for a NewMultipartUpload operation to work.");
        Populate();
    }

    private void Populate()
    {
        //Concat as Headers may have byte range info .etc.
        if (!ReplaceMetadataDirective && SourceObjectInfo.MetaData != null && SourceObjectInfo.MetaData.Count > 0)
            Headers = SourceObjectInfo.MetaData.Concat(Headers).GroupBy(item => item.Key)
                .ToDictionary(item => item.Key, item => item.First().Value);
        else if (ReplaceMetadataDirective) Headers = Headers ?? new Dictionary<string, string>();
        if (Headers != null)
        {
            var newKVList = new List<Tuple<string, string>>();
            foreach (var item in Headers)
            {
                var key = item.Key;
                if (!OperationsUtil.IsSupportedHeader(item.Key) &&
                    !item.Key.StartsWith("x-amz-meta", StringComparison.OrdinalIgnoreCase) &&
                    !OperationsUtil.IsSSEHeader(key))
                    newKVList.Add(new Tuple<string, string>("x-amz-meta-" + key.ToLowerInvariant(), item.Value));
            }

            foreach (var item in newKVList) Headers[item.Item1] = item.Item2;
        }
    }

    public new NewMultipartUploadCopyArgs WithObjectLockMode(RetentionMode mode)
    {
        base.WithObjectLockMode(mode);
        return this;
    }

    public new NewMultipartUploadCopyArgs WithHeaders(Dictionary<string, string> headers)
    {
        base.WithHeaders(headers);
        return this;
    }

    public new NewMultipartUploadCopyArgs WithObjectLockRetentionDate(DateTime untilDate)
    {
        base.WithObjectLockRetentionDate(untilDate);
        return this;
    }

    internal NewMultipartUploadCopyArgs WithStorageClass(string storageClass)
    {
        StorageClass = storageClass;
        return this;
    }

    internal NewMultipartUploadCopyArgs WithReplaceMetadataDirective(bool replace)
    {
        ReplaceMetadataDirective = replace;
        return this;
    }

    internal NewMultipartUploadCopyArgs WithReplaceTagsDirective(bool replace)
    {
        ReplaceTagsDirective = replace;
        return this;
    }

    public NewMultipartUploadCopyArgs WithSourceObjectInfo(ObjectStat stat)
    {
        SourceObjectInfo = stat;
        return this;
    }

    public NewMultipartUploadCopyArgs WithCopyObjectSource(CopySourceObjectArgs cs)
    {
        if (cs == null)
            throw new InvalidOperationException("The copy source object needed for copy operation is not initialized.");

        SourceObject = SourceObject ?? new CopySourceObjectArgs();
        SourceObject.RequestMethod = HttpMethod.Put;
        SourceObject.BucketName = cs.BucketName;
        SourceObject.ObjectName = cs.ObjectName;
        SourceObject.VersionId = cs.VersionId;
        SourceObject.SSE = cs.SSE;
        SourceObject.Headers = cs.Headers;
        SourceObject.MatchETag = cs.MatchETag;
        SourceObject.ModifiedSince = cs.ModifiedSince;
        SourceObject.NotMatchETag = cs.NotMatchETag;
        SourceObject.UnModifiedSince = cs.UnModifiedSince;
        SourceObject.CopySourceObjectPath = $"{cs.BucketName}/{utils.UrlEncode(cs.ObjectName)}";
        SourceObject.CopyOperationConditions = cs.CopyOperationConditions?.Clone();
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploads", "");
        if (ObjectTags != null && ObjectTags.TaggingSet != null
                               && ObjectTags.TaggingSet.Tag.Count > 0)
        {
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging", ObjectTags.GetTagString());
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging-directive",
                ReplaceTagsDirective ? "REPLACE" : "COPY");
        }

        if (ReplaceMetadataDirective)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-metadata-directive", "REPLACE");
        if (!string.IsNullOrWhiteSpace(StorageClass))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-storage-class", StorageClass);
        if (ObjectLockSet)
        {
            if (!RetentionUntilDate.Equals(default))
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                    utils.To8601String(RetentionUntilDate));
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode",
                ObjectLockRetentionMode == RetentionMode.GOVERNANCE ? "GOVERNANCE" : "COMPLIANCE");
        }

        return requestMessageBuilder;
    }
}

internal class CompleteMultipartUploadArgs : ObjectWriteArgs<CompleteMultipartUploadArgs>
{
    internal CompleteMultipartUploadArgs()
    {
        RequestMethod = HttpMethod.Post;
    }

    internal CompleteMultipartUploadArgs(MultipartCopyUploadArgs args)
    {
        // destBucketName, destObjectName, metadata, sseHeaders
        RequestMethod = HttpMethod.Post;
        BucketName = args.BucketName;
        ObjectName = args.ObjectName ?? args.SourceObject.ObjectName;
        Headers = new Dictionary<string, string>();
        SSE = args.SSE;
        SSE?.Marshal(args.Headers);
        if (args.Headers != null && args.Headers.Count > 0)
            Headers = Headers.Concat(args.Headers).GroupBy(item => item.Key)
                .ToDictionary(item => item.Key, item => item.First().Value);
    }

    internal string UploadId { get; set; }
    internal Dictionary<int, string> ETags { get; set; }

    internal override void Validate()
    {
        base.Validate();
        if (string.IsNullOrWhiteSpace(UploadId))
            throw new ArgumentNullException(nameof(UploadId) + " cannot be empty.");
        if (ETags == null || ETags.Count <= 0)
            throw new InvalidOperationException(nameof(ETags) + " dictionary cannot be empty.");
    }

    internal CompleteMultipartUploadArgs WithUploadId(string uploadId)
    {
        UploadId = uploadId;
        return this;
    }

    internal CompleteMultipartUploadArgs WithETags(Dictionary<int, string> etags)
    {
        if (etags != null && etags.Count > 0) ETags = new Dictionary<int, string>(etags);
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploadId", $"{UploadId}");
        var parts = new List<XElement>();

        for (var i = 1; i <= ETags.Count; i++)
            parts.Add(new XElement("Part",
                new XElement("PartNumber", i),
                new XElement("ETag", ETags[i])));
        var completeMultipartUploadXml = new XElement("CompleteMultipartUpload", parts);
        var bodyString = completeMultipartUploadXml.ToString();
        var body = Encoding.UTF8.GetBytes(bodyString);
        var bodyInBytes = Encoding.UTF8.GetBytes(bodyString);
        requestMessageBuilder.BodyParameters.Add("content-type", "application/xml");
        requestMessageBuilder.SetBody(bodyInBytes);
        // var bodyInCharArr = Encoding.UTF8.GetString(requestMessageBuilder.Content).ToCharArray();

        return requestMessageBuilder;
    }
}

internal class PutObjectPartArgs : PutObjectArgs
{
    public PutObjectPartArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal override void Validate()
    {
        base.Validate();
        if (string.IsNullOrWhiteSpace(UploadId))
            throw new ArgumentNullException(nameof(UploadId) + " not assigned for PutObjectPart operation.");
    }

    public new PutObjectPartArgs WithBucket(string bkt)
    {
        return (PutObjectPartArgs)base.WithBucket(bkt);
    }

    public new PutObjectPartArgs WithObject(string obj)
    {
        return (PutObjectPartArgs)base.WithObject(obj);
    }

    public new PutObjectPartArgs WithObjectSize(long size)
    {
        return (PutObjectPartArgs)base.WithObjectSize(size);
    }

    public new PutObjectPartArgs WithHeaders(Dictionary<string, string> hdr)
    {
        return (PutObjectPartArgs)base.WithHeaders(hdr);
    }

    public PutObjectPartArgs WithRequestBody(object data)
    {
        return (PutObjectPartArgs)base.WithRequestBody(utils.ObjectToByteArray(data));
    }

    public new PutObjectPartArgs WithStreamData(Stream data)
    {
        return (PutObjectPartArgs)base.WithStreamData(data);
    }

    public new PutObjectPartArgs WithContentType(string type)
    {
        return (PutObjectPartArgs)base.WithContentType(type);
    }

    public new PutObjectPartArgs WithUploadId(string id)
    {
        return (PutObjectPartArgs)base.WithUploadId(id);
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        return requestMessageBuilder;
    }
}

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

    internal override void Validate()
    {
        base.Validate();
        // Check atleast one of filename or stream are initialized
        if (string.IsNullOrWhiteSpace(FileName) && ObjectStreamData == null)
            throw new ArgumentException("One of " + nameof(FileName) + " or " + nameof(ObjectStreamData) +
                                        " must be set.");
        if (PartNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(PartNumber), PartNumber,
                "Invalid Part number value. Cannot be less than 0");
        // Check if only one of filename or stream are initialized
        if (!string.IsNullOrWhiteSpace(FileName) && ObjectStreamData != null)
            throw new ArgumentException("Only one of " + nameof(FileName) + " or " + nameof(ObjectStreamData) +
                                        " should be set.");
        if (!string.IsNullOrWhiteSpace(FileName)) utils.ValidateFile(FileName);
        // Check object size when using stream data
        if (ObjectStreamData != null && ObjectSize == 0)
            throw new ArgumentException($"{nameof(ObjectSize)} must be set");
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

        if (ObjectTags != null && ObjectTags.TaggingSet != null
                               && ObjectTags.TaggingSet.Tag.Count > 0)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging", ObjectTags.GetTagString());
        if (Retention != null)
        {
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                Retention.RetainUntilDate);
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode", Retention.Mode.ToString());
            requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
                utils.getMD5SumStr(RequestBody));
        }

        if (LegalHoldEnabled != null)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-legal-hold",
                LegalHoldEnabled == true ? "ON" : "OFF");
        if (RequestBody != null)
        {
            var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(RequestBody);
            var hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", hex);
            requestMessageBuilder.SetBody(RequestBody);
        }

        return requestMessageBuilder;
    }

    public new PutObjectArgs WithHeaders(Dictionary<string, string> metaData)
    {
        var sseHeaders = new Dictionary<string, string>();
        Headers = Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (metaData != null)
            foreach (var p in metaData)
            {
                var key = p.Key;
                if (!OperationsUtil.IsSupportedHeader(p.Key) &&
                    !p.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase) &&
                    !OperationsUtil.IsSSEHeader(p.Key))
                {
                    key = "x-amz-meta-" + key.ToLowerInvariant();
                    Headers.Remove(p.Key);
                }

                Headers[key] = p.Value;
                if (key == "Content-Type")
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

    ~PutObjectArgs()
    {
        if (!string.IsNullOrWhiteSpace(FileName) && ObjectStreamData != null)
            ObjectStreamData.Close();
    }
}