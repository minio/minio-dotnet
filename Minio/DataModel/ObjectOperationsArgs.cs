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
using System.Collections.Generic;
using System.IO;
using RestSharp;
using System.Globalization;
using System.Xml.Linq;
using System.Xml;
using System.Linq;

using Minio.DataModel;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio
{
    public class SelectObjectContentArgs: EncryptionArgs<SelectObjectContentArgs>
    {
        private SelectObjectOptions SelectOptions;

        public SelectObjectContentArgs()
        {
            this.RequestMethod = Method.POST;
            this.SelectOptions = new SelectObjectOptions();
        }

        public override void Validate()
        {
            base.Validate();
            if (string.IsNullOrEmpty(this.SelectOptions.Expression))
            {
                throw new InvalidOperationException("The Expression " + nameof(this.SelectOptions.Expression) + " for Select Object Content cannot be empty.");
            }
            if ((this.SelectOptions.InputSerialization == null) || (this.SelectOptions.OutputSerialization == null))
            {
                throw new InvalidOperationException("The Input/Output serialization members for SelectObjectContentArgs should be initialized " + nameof(this.SelectOptions.InputSerialization) + " " + nameof(this.SelectOptions.OutputSerialization));
            }
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            if (this.RequestBody == null)
            {
                this.RequestBody = System.Text.Encoding.UTF8.GetBytes(this.SelectOptions.MarshalXML());
            }
            request.AddQueryParameter("select","");
            request.AddQueryParameter("select-type","2");
            request.AddParameter("application/xml", (byte[])this.RequestBody, ParameterType.RequestBody);
            return request;
        }

        public SelectObjectContentArgs WithExpressionType(QueryExpressionType e)
        {
            this.SelectOptions.ExpressionType = e;
            return this;
        }

        public SelectObjectContentArgs WithQueryExpression(string expr)
        {
            this.SelectOptions.Expression = expr;
            return this;
        }

        public SelectObjectContentArgs WithInputSerialization(SelectObjectInputSerialization serialization)
        {
            this.SelectOptions.InputSerialization = serialization;
            return this;
        }

        public SelectObjectContentArgs WithOutputSerialization(SelectObjectOutputSerialization serialization)
        {
            this.SelectOptions.OutputSerialization = serialization;
            return this;
        }

        public SelectObjectContentArgs WithRequestProgress(RequestProgress requestProgress)
        {
            this.SelectOptions.RequestProgress = requestProgress;
            return this;
        }
    }

    public class ListIncompleteUploadsArgs : BucketArgs<ListIncompleteUploadsArgs>
    {
        internal string Prefix { get; private set; }
        internal string Delimiter { get; private set; }
        internal bool Recursive { get; private set; }
        public ListIncompleteUploadsArgs()
        {
            this.RequestMethod = Method.GET;
            this.Recursive = true;
        }
        public ListIncompleteUploadsArgs WithPrefix(string prefix)
        {
            this.Prefix = prefix ?? string.Empty;
            return this;
        }

        public ListIncompleteUploadsArgs WithDelimiter(string delim)
        {
            this.Delimiter = delim ?? string.Empty;
            return this;
        }

        public ListIncompleteUploadsArgs WithRecursive(bool recursive)
        {
            this.Recursive = recursive;
            this.Delimiter = (recursive)? string.Empty : "/";
            return this;
        }
    }

    public class GetMultipartUploadsListArgs : BucketArgs<GetMultipartUploadsListArgs>
    {
        internal string Prefix { get; private set; }
        internal string Delimiter { get; private set; }
        internal string KeyMarker { get; private set; }
        internal string UploadIdMarker { get; private set; }
        internal uint MAX_UPLOAD_COUNT { get; private set; }
        public GetMultipartUploadsListArgs()
        {
            this.RequestMethod = Method.GET;
            this.MAX_UPLOAD_COUNT = 1000;
        }
        public GetMultipartUploadsListArgs WithPrefix(string prefix)
        {
            this.Prefix = prefix ?? string.Empty;
            return this;
        }

        public GetMultipartUploadsListArgs WithDelimiter(string delim)
        {
            this.Delimiter = delim ?? string.Empty;
            return this;
        }

        public GetMultipartUploadsListArgs WithKeyMarker(string nextKeyMarker)
        {
            this.KeyMarker = nextKeyMarker ?? string.Empty;
            return this;
        }

        public GetMultipartUploadsListArgs WithUploadIdMarker(string nextUploadIdMarker)
        {
            this.UploadIdMarker = nextUploadIdMarker ?? string.Empty;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request.AddQueryParameter("uploads","");
            request.AddQueryParameter("prefix",this.Prefix);
            request.AddQueryParameter("delimiter",this.Delimiter);
            request.AddQueryParameter("key-marker",this.KeyMarker);
            request.AddQueryParameter("upload-id-marker",this.UploadIdMarker);
            request.AddQueryParameter("max-uploads",this.MAX_UPLOAD_COUNT.ToString());
            return request;
        }
    }

    public class PresignedGetObjectArgs : ObjectArgs<PresignedGetObjectArgs>
    {
        internal int Expiry { get; set; }
        internal DateTime? RequestDate { get; set; }

        public PresignedGetObjectArgs()
        {
            this.RequestMethod = Method.GET;
        }

        public override void Validate()
        {
            base.Validate();
            if (!utils.IsValidExpiry(this.Expiry))
            {
                throw new InvalidExpiryRangeException("expiry range should be between 1 and " + Constants.DefaultExpiryTime.ToString());
            }
        }

        public PresignedGetObjectArgs WithExpiry(int expiry)
        {
            this.Expiry = expiry;
            return this;
        }

        public PresignedGetObjectArgs WithRequestDate(DateTime? d)
        {
            this.RequestDate = d;
            return this;
        }
    }

    public class StatObjectArgs : ObjectQueryArgs<StatObjectArgs>
    {
        public StatObjectArgs()
        {
            this.RequestMethod = Method.HEAD;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            if (!string.IsNullOrEmpty(this.MatchETag))
            {
                request.AddOrUpdateParameter("If-Match", this.MatchETag, ParameterType.HttpHeader);
            }
            if (!string.IsNullOrEmpty(this.NotMatchETag))
            {
                request.AddOrUpdateParameter("If-None-Match", this.NotMatchETag, ParameterType.HttpHeader);
            }
            if (this.ModifiedSince != null && this.ModifiedSince != default(DateTime))
            {
                request.AddOrUpdateParameter("If-Modified-Since", this.ModifiedSince, ParameterType.HttpHeader);
            }
            if(this.UnModifiedSince != null && this.UnModifiedSince != default(DateTime))
            {
                request.AddOrUpdateParameter("If-Unmodified-Since", this.UnModifiedSince, ParameterType.HttpHeader);
            }
            
            return request;
        }

        public override void Validate()
        {
            base.Validate();
            if (!string.IsNullOrEmpty(this.NotMatchETag) && !string.IsNullOrEmpty(this.MatchETag))
            {
                throw new InvalidOperationException("Invalid to set both Etag match conditions " + nameof(this.NotMatchETag) + " and " + nameof(this.MatchETag));
            }
            if ((this.ModifiedSince != null && !this.ModifiedSince.Equals(default(DateTime))) &&
                    (this.ModifiedSince != null && !this.UnModifiedSince.Equals(default(DateTime))))
            {
                throw new InvalidOperationException("Invalid to set both modified date match conditions " + nameof(this.ModifiedSince) + " and " + nameof(this.UnModifiedSince));
            }
        }
    }


    public class PresignedPostPolicyArgs : ObjectArgs<PresignedPostPolicyArgs>
    {
        internal PostPolicy Policy { get; set; }
        internal DateTime Expiration { get; set; }

        internal string Region { get; set; }
        public new void Validate()
        {
            bool checkPolicy = false;
            try
            {
                utils.ValidateBucketName(this.BucketName);
                utils.ValidateObjectName(this.ObjectName);
            }
            catch (Exception ex)
            {
                if (ex is InvalidBucketNameException || ex is InvalidObjectNameException)
                {
                    checkPolicy = true;
                }
                else
                {
                    throw;
                }
            }
            if (checkPolicy)
            {
                if (!this.Policy.IsBucketSet())
                {
                    throw new InvalidOperationException("For the " + nameof(Policy) + " bucket should be set");
                }

                if (!this.Policy.IsKeySet())
                {
                    throw new InvalidOperationException("For the " + nameof(Policy) + " key should be set");
                }

                if (!this.Policy.IsExpirationSet())
                {
                    throw new InvalidOperationException("For the " + nameof(Policy) + " expiration should be set");
                }
                this.BucketName = this.Policy.Bucket;
                this.ObjectName = this.Policy.Key;
            }
            if (string.IsNullOrEmpty(this.Expiration.ToString()))
            {
                throw new InvalidOperationException("For the " + nameof(Policy) + " expiration should be set");
            }

        }

        public PresignedPostPolicyArgs WithExpiration(DateTime ex)
        {
            this.Expiration = ex;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            return request;
        }

        internal PresignedPostPolicyArgs WithRegion(string region)
        {
            this.Region = region;
            return this;
        }

        internal PresignedPostPolicyArgs WithSessionToken(string sessionToken)
        {
            this.Policy.SetSessionToken(sessionToken);
            return this;
        }

        internal PresignedPostPolicyArgs WithCredential(string credential)
        {
            this.Policy.SetCredential(credential);
            return this;
        }

        internal PresignedPostPolicyArgs WithSignature(string signature)
        {
            this.Policy.SetSignature(signature);
            return this;
        }
        public PresignedPostPolicyArgs WithPolicy(PostPolicy policy)
        {
            this.Policy = policy;
            return this;
        }
    }

    public class PresignedPutObjectArgs : ObjectArgs<PresignedPutObjectArgs>
    {
        internal int Expiry { get; set; }

        public PresignedPutObjectArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        internal new void Validate()
        {
            base.Validate();
            if (!utils.IsValidExpiry(this.Expiry))
            {
                throw new InvalidExpiryRangeException("Expiry range should be between 1 seconds and " + Constants.DefaultExpiryTime.ToString() + " seconds");
            }
        }

        public PresignedPutObjectArgs WithExpiry(int ex)
        {
            this.Expiry = ex;
            return this;
        }
    }

    public class RemoveUploadArgs : EncryptionArgs<RemoveUploadArgs>
    {
        internal string UploadId { get; private set; }
        public RemoveUploadArgs()
        {
            this.RequestMethod = Method.DELETE;
        }

        public RemoveUploadArgs WithUploadId(string id)
        {
            this.UploadId = id;
            return this;
        }

        public override void Validate()
        {
            base.Validate();
            if(string.IsNullOrEmpty(this.UploadId))
            {
                throw new InvalidOperationException(nameof(UploadId) + " cannot be empty. Please assign a valid upload ID to remove.");
            }
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request.AddQueryParameter("uploadId",$"{this.UploadId}");
            return request;
        }
    }

    public class RemoveIncompleteUploadArgs : EncryptionArgs<RemoveIncompleteUploadArgs>
    {
        public RemoveIncompleteUploadArgs()
        {
            this.RequestMethod = Method.DELETE;
        }
    }


    public class GetObjectLegalHoldArgs : ObjectVersionArgs<GetObjectLegalHoldArgs>
    {
        public GetObjectLegalHoldArgs()
        {
            this.RequestMethod = Method.GET;
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("legal-hold", "");
            if( !string.IsNullOrEmpty(this.VersionId) )
            {
                request.AddQueryParameter("versionId", this.VersionId);
            }
            return request;
        }
    }

    public class SetObjectLegalHoldArgs : ObjectVersionArgs<SetObjectLegalHoldArgs>
    {
        internal bool LegalHoldON { get; private set; }

        public SetObjectLegalHoldArgs()
        {
            this.RequestMethod = Method.PUT;
            this.LegalHoldON = false;
        }

        public SetObjectLegalHoldArgs WithLegalHold(bool status)
        {
            this.LegalHoldON = status;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("legal-hold", "");
            if( !string.IsNullOrEmpty(this.VersionId) )
            {
                request.AddQueryParameter("versionId", this.VersionId);
            }
            ObjectLegalHoldConfiguration config = new ObjectLegalHoldConfiguration(this.LegalHoldON);
            string body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
            request.AddParameter("text/xml", body, ParameterType.RequestBody);
            request.AddOrUpdateParameter("Content-MD5",
                                          utils.getMD5SumStr(System.Text.Encoding.UTF8.GetBytes(body)),
                                          ParameterType.HttpHeader);
            return request;
        }
    }

    public class GetObjectArgs : ObjectQueryArgs<GetObjectArgs>
    {
        internal Action<Stream> CallBack { get; private set; }
        internal long ObjectOffset { get; private set; }
        internal long ObjectLength { get; private set; }
        internal string FileName { get; private set; }
        internal bool OffsetLengthSet { get; set; }

        public GetObjectArgs()
        {
            this.RequestMethod = Method.GET;
            this.OffsetLengthSet = false;
        }

        public override void Validate()
        {
            base.Validate();
            if (this.CallBack == null)
            {
                throw new MinioException("CallBack method not set of GetObject operation.");
            }
            if (OffsetLengthSet)
            {
                if (this.ObjectOffset < 0)
                {
                    throw new ArgumentException("Offset should be zero or greater", nameof(this.ObjectOffset));
                }

                if (this.ObjectLength < 0)
                {
                    throw new ArgumentException("Length should be greater than or equal to zero", nameof(this.ObjectLength));
                }
            }
            if (this.FileName != null)
            {
                utils.ValidateFile(this.FileName);
            }
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            var headers = new Dictionary<string, string>();
            if (this.SSE != null && this.SSE.GetType().Equals(EncryptionType.SSE_C))
            {
                this.SSE.Marshal(headers);
            }
            request.ResponseWriter = this.CallBack;

            return request;
        }   


        public GetObjectArgs WithCallbackStream(Action<Stream> cb)
        {
            this.CallBack = cb;
            return this;
        }

        public GetObjectArgs WithLengthAndOffset(long offset, long length)
        {
            this.OffsetLengthSet = true;
            this.ObjectOffset = offset;
            this.ObjectLength = length;
            if (ObjectLength > 0)
            {
                this.HeaderMap.Add("Range", "bytes=" + offset.ToString() + "-" + (offset + length - 1).ToString());
            }
            return this;
        }

        public GetObjectArgs WithFile(string file)
        {
            this.FileName = file;
            return this;
        }
    }

    public class RemoveObjectArgs : ObjectArgs<RemoveObjectArgs>
    {
        public string VersionId { get; private set; }

        public RemoveObjectArgs()
        {
            this.RequestMethod = Method.DELETE;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            if (!string.IsNullOrEmpty(this.VersionId))
            {
                request.AddQueryParameter("versionId",$"{this.VersionId}");
            }
            return request;
        }
        public RemoveObjectArgs WithVersionId(string ver)
        {
            this.VersionId = ver;
            return this;
        }
    }

    public class RemoveObjectsArgs : ObjectArgs<RemoveObjectsArgs>
    {
        internal List<string> ObjectNames { get; private set; }
        // Each element in the list is a Tuple. Each Tuple has an Object name & the version ID.
        internal List<Tuple<string, string>> ObjectNamesVersions  { get; private set; }

        public RemoveObjectsArgs()
        {
            this.ObjectName = null;
            this.ObjectNames = new List<string>();
            this.ObjectNamesVersions = new List<Tuple<string, string>>();
            this.RequestMethod = Method.POST;
        }

        public RemoveObjectsArgs WithObjectAndVersions(string objectName, List<string> versions)
        {
            foreach (var vid in versions)
            {
                this.ObjectNamesVersions.Add(new Tuple<string, string>(objectName, vid));
            }
            return this;
        }

        // Tuple<string, List<string>>. Tuple object name -> List of Version IDs.
        public RemoveObjectsArgs WithObjectsVersions(List<Tuple<string, List<string>>> objectsVersionsList)
        {
            foreach (var objVersions in objectsVersionsList)
            {
                foreach (var vid in objVersions.Item2)
                {
                    this.ObjectNamesVersions.Add(new Tuple<string, string>(objVersions.Item1, vid));
                }
            }
            return this;
        }

        public RemoveObjectsArgs WithObjectsVersions(List<Tuple<string, string>> objectVersions)
        {
            this.ObjectNamesVersions.AddRange(objectVersions);
            return this;
        }

        public RemoveObjectsArgs WithObjects(List<string> names)
        {
            this.ObjectNames = names;
            return this;
        }

        public override void Validate()
        {
            // Skip object name validation.
            utils.ValidateBucketName(this.BucketName);
            if (!string.IsNullOrEmpty(this.ObjectName))
            {
                throw new InvalidOperationException(nameof(ObjectName)  + " is set. Please use " + nameof(WithObjects) + "or " +
                    nameof(WithObjectsVersions) + " method to set objects to be deleted.");
            }
            if ((this.ObjectNames == null && this.ObjectNamesVersions == null) ||
                (this.ObjectNames.Count == 0 && this.ObjectNamesVersions.Count == 0))
            {
                throw new InvalidOperationException("Please assign list of object names or object names and version IDs to remove using method(s) " +
                    nameof(WithObjects) + " " + nameof(WithObjectsVersions));
            }
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            List<XElement> objects = new List<XElement>();
            request.AddQueryParameter("delete","");
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            request.RequestFormat = DataFormat.Xml;
            if (this.ObjectNamesVersions.Count > 0)
            {
                // Object(s) & multiple versions
                foreach (var objTuple in this.ObjectNamesVersions)
                {
                    objects.Add(new XElement("Object",
                                        new XElement("Key", objTuple.Item1),
                                        new XElement("VersionId", objTuple.Item2)));
                }
                var deleteObjectsRequest = new XElement("Delete", objects,
                                                new XElement("Quiet", true));
                request.AddXmlBody(deleteObjectsRequest);
            }
            else
            {
                // Multiple Objects
                foreach (var obj in this.ObjectNames)
                {
                    objects.Add(new XElement("Object",
                                        new XElement("Key", obj)));
                }
                var deleteObjectsRequest = new XElement("Delete", objects,
                                                new XElement("Quiet", true));
                request.AddXmlBody(deleteObjectsRequest);
            }
            return request;
        }
    }

    public class SetObjectTagsArgs : ObjectVersionArgs<SetObjectTagsArgs>
    {
        internal Dictionary<string, string> TagKeyValuePairs { get; set; }
        internal Tagging ObjectTags { get; private set; }
        public SetObjectTagsArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        public SetObjectTagsArgs WithTagKeyValuePairs(Dictionary<string, string> kv)
        {
            this.TagKeyValuePairs = new Dictionary<string, string>(kv);
            this.ObjectTags = Tagging.GetBucketTags(kv);
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("tagging","");
            if (!string.IsNullOrEmpty(this.VersionId))
            {
                request.AddQueryParameter("versionId", this.VersionId);
            }
            string body = this.ObjectTags.MarshalXML();
            request.AddParameter(new Parameter("text/xml", body, ParameterType.RequestBody));

            return request;
        }

        public override void Validate()
        {
            base.Validate();
            if (this.TagKeyValuePairs == null || this.TagKeyValuePairs.Count == 0)
            {
                throw new InvalidOperationException("Unable to set empty tags.");
            }
        }
    }

    public class GetObjectTagsArgs : ObjectVersionArgs<GetObjectTagsArgs>
    {
        public GetObjectTagsArgs()
        {
            this.RequestMethod = Method.GET;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("tagging","");
            if (!string.IsNullOrEmpty(this.VersionId))
            {
                request.AddQueryParameter("versionId", this.VersionId);
            }
            return request;
        }
    }

    public class RemoveObjectTagsArgs : ObjectVersionArgs<RemoveObjectTagsArgs>
    {
        public RemoveObjectTagsArgs()
        {
            this.RequestMethod = Method.DELETE;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("tagging","");
            if (!string.IsNullOrEmpty(this.VersionId))
            {
                request.AddQueryParameter("versionId", this.VersionId);
            }
            return request;
        }
    }

    public class SetObjectRetentionArgs : ObjectVersionArgs<SetObjectRetentionArgs>
    {
        internal bool BypassGovernanceMode { get; set; }
        internal RetentionMode Mode { get; set; }
        internal DateTime RetentionUntilDate { get; set; }

        public SetObjectRetentionArgs()
        {
            this.RequestMethod = Method.PUT;
            this.RetentionUntilDate = default(DateTime);
            this.Mode = RetentionMode.GOVERNANCE;
        }

        public override void Validate()
        {
            base.Validate();
            if (this.RetentionUntilDate.Equals(default(DateTime)))
            {
                throw new InvalidOperationException("Retention Period is not set. Please set using " +
                        nameof(WithRetentionUntilDate) + ".");
            }
            if (DateTime.Compare(this.RetentionUntilDate, DateTime.Now)  <= 0)
            {
                throw new InvalidOperationException("Retention until date set using " + nameof(WithRetentionUntilDate) + " needs to be in the future.");
            }
        }
        public SetObjectRetentionArgs WithBypassGovernanceMode(bool bypass = true)
        {
            this.BypassGovernanceMode = bypass;
            return this;
        }

        public SetObjectRetentionArgs WithRetentionMode(RetentionMode mode)
        {
            this.Mode = mode;
            return this;
        }

        public SetObjectRetentionArgs WithRetentionUntilDate(DateTime date)
        {
            this.RetentionUntilDate = date;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("retention", "");
            if( !string.IsNullOrEmpty(this.VersionId) )
            {
                request.AddQueryParameter("versionId", this.VersionId);
            }
            if (this.BypassGovernanceMode)
            {
                request.AddOrUpdateParameter("x-amz-bypass-governance-retention", "true", ParameterType.HttpHeader);
            }
            ObjectRetentionConfiguration config = new ObjectRetentionConfiguration(this.RetentionUntilDate, this.Mode);
            string body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
            request.AddParameter("text/xml", body, ParameterType.RequestBody);
            request.AddOrUpdateParameter("Content-MD5",
                                          utils.getMD5SumStr(System.Text.Encoding.UTF8.GetBytes(body)),
                                          ParameterType.HttpHeader);
            return request;
        }
    }

    public class GetObjectRetentionArgs : ObjectVersionArgs<GetObjectRetentionArgs>
    {
        public GetObjectRetentionArgs()
        {
            this.RequestMethod = Method.GET;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("retention", "");
            if( !string.IsNullOrEmpty(this.VersionId) )
            {
                request.AddQueryParameter("versionId", this.VersionId);
            }
            return request;
        }
    }

    public class ClearObjectRetentionArgs : ObjectVersionArgs<ClearObjectRetentionArgs>
    {
        public ClearObjectRetentionArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        public static string EmptyRetentionConfigXML()
        {
            StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            XmlWriter xw = XmlWriter.Create(sw, settings);
            xw.WriteStartElement("Retention");
            xw.WriteString("");
            xw.WriteFullEndElement();
            xw.Flush();
            return sw.ToString();
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("retention", "");
            if( !string.IsNullOrEmpty(this.VersionId) )
            {
                request.AddQueryParameter("versionId", this.VersionId);
            }
            // Required for Clear Object Retention.
            request.AddOrUpdateParameter("x-amz-bypass-governance-retention", "true", ParameterType.HttpHeader);
            string body = EmptyRetentionConfigXML();
            request.AddParameter("text/xml", body, ParameterType.RequestBody);
            request.AddOrUpdateParameter("Content-MD5",
                                          utils.getMD5SumStr(System.Text.Encoding.UTF8.GetBytes(body)),
                                          ParameterType.HttpHeader);
            return request;
        }
    }

    public class CopySourceObjectArgs : ObjectQueryArgs<CopySourceObjectArgs>
    {
        internal string CopySourceObjectPath { get; set; }
        internal CopyConditions CopyOperationConditions { get; set; }
        public CopySourceObjectArgs()
        {
            this.RequestMethod = Method.PUT;
            this.CopyOperationConditions = new CopyConditions();
        }

        public override void Validate()
        {
            base.Validate();
        }

        public CopySourceObjectArgs WithCopyConditions(CopyConditions cp)
        {
            this.CopyOperationConditions = (cp != null)? cp.Clone() : new CopyConditions();
            return this;
        }
    }

    internal class CopyObjectRequestArgs : ObjectWriteArgs<CopyObjectRequestArgs>
    {
        internal CopySourceObjectArgs CopySourceObject { get; set; }
        internal ObjectStat CopySourceObjectInfo { get; set; }
        internal Type CopyOperationObjectType { get; set; }
        internal bool ReplaceTagsDirective { get; set; }
        internal bool ReplaceMetadataDirective { get; set; }
        internal string StorageClass { get; set; }

        public CopyObjectRequestArgs(CopyObjectArgs cpArgs)
        {
            if (cpArgs == null || cpArgs.CopySourceObject == null)
            {
                string message = (cpArgs == null)? $"The constructor of " + nameof(CopyObjectRequestArgs) + "initialized with arguments of CopyObjectArgs null." :
                                                    $"The constructor of " + nameof(CopyObjectRequestArgs) + "initialized with arguments of CopyObjectArgs type but with " + nameof(cpArgs.CopySourceObject) + " not initialized.";
                throw new InvalidOperationException(message);
            }
            this.CopySourceObject = new CopySourceObjectArgs();
            this.CopySourceObject.BucketName = cpArgs.CopySourceObject.BucketName;
            this.CopySourceObject.ObjectName = cpArgs.CopySourceObject.ObjectName;
            this.CopySourceObject.VersionId = cpArgs.CopySourceObject.VersionId;
            this.CopySourceObject.CopyOperationConditions = cpArgs.CopySourceObject.CopyOperationConditions.Clone();
            if (cpArgs.CopySourceObject.HeaderMap != null)
            {
                this.CopySourceObject.HeaderMap = this.CopySourceObject.HeaderMap ?? new Dictionary<string, string>();
                this.CopySourceObject.HeaderMap = this.CopySourceObject.HeaderMap.Concat(cpArgs.CopySourceObject.HeaderMap).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            this.HeaderMap = (cpArgs.HeaderMap != null)?new Dictionary<string, string>(cpArgs.HeaderMap):new Dictionary<string, string>();
            this.CopySourceObjectInfo = cpArgs.CopySourceObjectInfo;
            if (cpArgs.CopySourceObjectInfo.MetaData != null && cpArgs.CopySourceObjectInfo.MetaData.Count > 0)
            {
                this.HeaderMap = this.HeaderMap.Concat(cpArgs.CopySourceObjectInfo.MetaData).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            this.CopySourceObject.SSE = cpArgs.CopySourceObject.SSE;
            this.RequestMethod = Method.PUT;
            this.BucketName = cpArgs.BucketName;
            this.ObjectName = cpArgs.ObjectName;
            this.HeaderMap = cpArgs.HeaderMap ?? new Dictionary<string, string>();
            if (this.CopySourceObject.HeaderMap != null)
            {
                this.HeaderMap = this.HeaderMap.Concat(this.CopySourceObject.HeaderMap).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }

            this.RequestBody = cpArgs.RequestBody;
            this.SSE = cpArgs.SSE;
            this.SSEHeaders = cpArgs.SSEHeaders ?? new Dictionary<string, string>();
            if (this.CopySourceObject.SSEHeaders != null)
            {
                this.SSEHeaders = this.SSEHeaders.Concat(this.CopySourceObject.SSEHeaders).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            this.VersionId = cpArgs.VersionId;
            this.ObjectTags = (cpArgs.ObjectTags != null && cpArgs.ObjectTags.TaggingSet.Tag.Count > 0)?cpArgs.ObjectTags:null;
            this.ReplaceTagsDirective = cpArgs.ReplaceTagsDirective;
        }

        public CopyObjectRequestArgs(MultipartCopyUploadArgs mcArgs)
        {
            if (mcArgs == null)
            {
                throw new InvalidOperationException("The copy source object needed for copy operation is not initialized.");
            }
            mcArgs.Validate();

            this.CopySourceObject = new CopySourceObjectArgs();
            this.CopySourceObject.BucketName = mcArgs.BucketName;
            this.CopySourceObject.ObjectName = mcArgs.ObjectName;
            this.CopySourceObject.HeaderMap = mcArgs.HeaderMap;
            this.CopySourceObject.MatchETag = mcArgs.CopySourceObject.MatchETag;
            this.CopySourceObject.ModifiedSince = mcArgs.CopySourceObject.ModifiedSince;
            this.CopySourceObject.NotMatchETag = mcArgs.CopySourceObject.NotMatchETag;
            this.CopySourceObject.UnModifiedSince = mcArgs.CopySourceObject.UnModifiedSince;
            this.CopySourceObject.CopyOperationConditions = mcArgs.CopySourceObject.CopyOperationConditions;

            this.BucketName = mcArgs.BucketName;
            this.ObjectName = mcArgs.ObjectName ?? mcArgs.CopySourceObject.ObjectName;

            this.HeaderMap = mcArgs.HeaderMap;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            string sourceObjectPath = this.CopySourceObject.BucketName + "/" + utils.UrlEncode(this.CopySourceObject.ObjectName);
            if(!string.IsNullOrEmpty(this.CopySourceObject.VersionId))
            {
                sourceObjectPath += "?versionId=" + this.CopySourceObject.VersionId;
            }
            // Set the object source
            request.AddHeader("x-amz-copy-source", sourceObjectPath);

            if (this.HeaderMap != null)
            {
                foreach (var hdr in this.HeaderMap)
                {
                    request.AddQueryParameter(hdr.Key,hdr.Value);
                }
            }

            if (this.CopySourceObject.CopyOperationConditions != null)
            {
                foreach (var item in this.CopySourceObject.CopyOperationConditions.GetConditions())
                {
                    request.AddHeader(item.Key, item.Value);
                }
            }
            if (!string.IsNullOrEmpty(this.MatchETag))
            {
                request.AddOrUpdateParameter("x-amz-copy-source-if-match", this.MatchETag, ParameterType.HttpHeader);
            }
            if (!string.IsNullOrEmpty(this.NotMatchETag))
            {
                request.AddOrUpdateParameter("x-amz-copy-source-if-none-match", this.NotMatchETag, ParameterType.HttpHeader);
            }
            if (this.ModifiedSince != null && this.ModifiedSince != default(DateTime))
            {
                request.AddOrUpdateParameter("x-amz-copy-source-if-unmodified-since", this.ModifiedSince, ParameterType.HttpHeader);
            }
            if(this.UnModifiedSince != null && this.UnModifiedSince != default(DateTime))
            {
                request.AddOrUpdateParameter("x-amz-copy-source-if-modified-since", this.UnModifiedSince, ParameterType.HttpHeader);
            }
            if (this.ObjectTags != null && this.ObjectTags.TaggingSet != null
                    && this.ObjectTags.TaggingSet.Tag.Count > 0)
            {
                request.AddOrUpdateParameter("x-amz-tagging", this.ObjectTags.GetTagString(), ParameterType.HttpHeader);
                request.AddOrUpdateParameter("x-amz-tagging-directive",
                            this.ReplaceTagsDirective?"REPLACE":"COPY",
                            ParameterType.HttpHeader);
            }
            if (this.ReplaceMetadataDirective)
            {
                request.AddOrUpdateParameter("x-amz-metadata-directive", "REPLACE", ParameterType.HttpHeader);
            }
            if (!string.IsNullOrEmpty(this.StorageClass))
            {
                request.AddOrUpdateParameter("x-amz-storage-class", this.StorageClass, ParameterType.HttpHeader);
            }

            return request;
        }

        public CopyObjectRequestArgs WithCopyOperationObjectType(Type cp)
        {
            this.CopyOperationObjectType = cp;
            return this;
        }

        public override void Validate()
        {
            base.Validate();
            if (this.CopySourceObject == null)
            {
                throw new InvalidOperationException(nameof(this.CopySourceObject) + " has not been assigned.");
            }
        }
    }

    public class CopyObjectArgs : ObjectWriteArgs<CopyObjectArgs>
    {
        internal CopySourceObjectArgs CopySourceObject { get; set; }
        internal ObjectStat CopySourceObjectInfo { get; set; }
        internal bool ReplaceTagsDirective { get; set; }
        internal bool ReplaceMetadataDirective { get; set; }
        internal string StorageClass { get; set; }

        public CopyObjectArgs()
        {
            this.RequestMethod = Method.PUT;
            this.CopySourceObject = new CopySourceObjectArgs();
            this.ReplaceTagsDirective = false;
        }

        public override void Validate()
        {
            // We don't need to call base validate.
            // If object name is empty we default to source object name.
            this.CopySourceObject.Validate();
            utils.ValidateBucketName(this.BucketName);
            if (this.CopySourceObject == null)
            {
                throw new InvalidOperationException(nameof(this.CopySourceObject) + " has not been assigned. Please use " + nameof(this.WithCopyObjectSource));
            }
            if (this.CopySourceObjectInfo == null)
            {
                throw new InvalidOperationException("StatObject result for the copy source object needed to continue copy operation. Use " + nameof(WithCopyObjectSourceStats) + " to initialize StatObject result.");
            }
        }

        public CopyObjectArgs WithCopyObjectSource(CopySourceObjectArgs cs)
        {
            if (cs == null)
            {
                throw new InvalidOperationException("The copy source object needed for copy operation is not initialized.");
            }
            cs.Validate();

            this.CopySourceObject = this.CopySourceObject ?? new CopySourceObjectArgs();
            this.CopySourceObject.BucketName = cs.BucketName;
            this.CopySourceObject.ObjectName = cs.ObjectName;
            this.CopySourceObject.VersionId = cs.VersionId;
            this.CopySourceObject.RequestMethod = Method.PUT;
            this.CopySourceObject.SSE = cs.SSE;
            this.CopySourceObject.SSEHeaders = cs.SSEHeaders;
            this.CopySourceObject.HeaderMap = cs.HeaderMap;
            this.CopySourceObject.MatchETag = cs.MatchETag;
            this.CopySourceObject.ModifiedSince = cs.ModifiedSince;
            this.CopySourceObject.NotMatchETag = cs.NotMatchETag;
            this.CopySourceObject.UnModifiedSince = cs.UnModifiedSince;
            this.CopySourceObject.CopySourceObjectPath = $"{cs.BucketName}/{utils.UrlEncode(cs.ObjectName)}";
            this.CopySourceObject.CopyOperationConditions = cs.CopyOperationConditions?.Clone(); 
            return this;
        }

        public CopyObjectArgs WithReplaceTagsDirective(bool replace)
        {
            this.ReplaceTagsDirective = replace;
            return this;
        }
        internal CopyObjectArgs WithCopyObjectSourceStats(ObjectStat info)
        {
            this.CopySourceObjectInfo = info;
            if (info.MetaData != null)
            {
                this.CopySourceObject.HeaderMap = this.CopySourceObject.HeaderMap ?? new Dictionary<string, string>();
                this.CopySourceObject.HeaderMap = this.CopySourceObject.HeaderMap.Concat(info.MetaData).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            return this;
        }

        public CopyObjectArgs WithStorageClass(string storageClass)
        {
            this.StorageClass = storageClass;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            foreach (var hdr in this.SSEHeaders)
            {
                this.HeaderMap[hdr.Key] = hdr.Value;
            }
            foreach (var hdr in this.CopySourceObject.SSEHeaders)
            {
                this.HeaderMap[hdr.Key] = hdr.Value;
            }
            if (!string.IsNullOrEmpty(this.MatchETag))
            {
                request.AddOrUpdateParameter("x-amz-copy-source-if-match", this.MatchETag, ParameterType.HttpHeader);
            }
            if (!string.IsNullOrEmpty(this.NotMatchETag))
            {
                request.AddOrUpdateParameter("x-amz-copy-source-if-none-match", this.NotMatchETag, ParameterType.HttpHeader);
            }
            if (this.ModifiedSince != null && this.ModifiedSince != default(DateTime))
            {
                request.AddOrUpdateParameter("x-amz-copy-source-if-unmodified-since", this.ModifiedSince, ParameterType.HttpHeader);
            }
            if(this.UnModifiedSince != null && this.UnModifiedSince != default(DateTime))
            {
                request.AddOrUpdateParameter("x-amz-copy-source-if-modified-since", this.UnModifiedSince, ParameterType.HttpHeader);
            }
            if( !string.IsNullOrEmpty(this.VersionId) )
            {
                request.AddQueryParameter("versionId", this.VersionId);
            }
            if (this.ObjectTags != null && this.ObjectTags.TaggingSet != null
                    && this.ObjectTags.TaggingSet.Tag.Count > 0)
            {
                request.AddOrUpdateParameter("x-amz-tagging", this.ObjectTags.GetTagString(), ParameterType.HttpHeader);
                request.AddOrUpdateParameter("x-amz-tagging-directive",
                            this.ReplaceTagsDirective?"COPY":"REPLACE",
                            ParameterType.HttpHeader);
            }
            if (this.ReplaceMetadataDirective)
            {
                request.AddOrUpdateParameter("x-amz-metadata-directive", "REPLACE", ParameterType.HttpHeader);
            }
            if (!string.IsNullOrEmpty(this.StorageClass))
            {
                request.AddOrUpdateParameter("x-amz-storage-class", this.StorageClass, ParameterType.HttpHeader);
            }
            return request;
        }
    }

    public class NewMultipartUploadArgs: EncryptionArgs<NewMultipartUploadArgs>
    {
        public NewMultipartUploadArgs()
        {
            this.RequestMethod = Method.POST;
        }

        public NewMultipartUploadArgs(MultipartCopyUploadArgs args)
        {
            // destBucketName, destObjectName, metadata, sseHeaders
            this.BucketName = args.BucketName;
            this.ObjectName = args.ObjectName;
            this.HeaderMap = args.HeaderMap;
            this.SSE = args.SSE;
            this.SSE?.Marshal(this.SSEHeaders);
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request.AddQueryParameter("uploads","");
            return request;
        }
    }

    public class MultipartCopyUploadArgs : ObjectVersionArgs<MultipartCopyUploadArgs>
    {
        internal CopySourceObjectArgs CopySourceObject { get; set; }
        internal ObjectStat CopySourceObjectInfo { get; set; }
        internal long CopySize { get; set; }
        public MultipartCopyUploadArgs(CopyObjectArgs cpArgs)
        {
             if (cpArgs == null || cpArgs.CopySourceObject == null)
            {
                string message = (cpArgs == null)? $"The constructor of " + nameof(CopyObjectRequestArgs) + "initialized with arguments of CopyObjectArgs null." :
                                                    $"The constructor of " + nameof(CopyObjectRequestArgs) + "initialized with arguments of CopyObjectArgs type but with " + nameof(cpArgs.CopySourceObject) + " not initialized.";
                throw new InvalidOperationException(message);
            }
            this.RequestMethod = Method.PUT;

            this.CopySourceObject = new CopySourceObjectArgs();
            this.CopySourceObject.BucketName = cpArgs.CopySourceObject.BucketName;
            this.CopySourceObject.ObjectName = cpArgs.CopySourceObject.ObjectName;
            this.CopySourceObject.CopyOperationConditions = cpArgs.CopySourceObject.CopyOperationConditions.Clone();
            if (cpArgs.CopySourceObject.HeaderMap != null)
            {
                this.CopySourceObject.HeaderMap = new Dictionary<string, string>();
                this.CopySourceObject.HeaderMap = this.CopySourceObject.HeaderMap.Concat(cpArgs.CopySourceObject.HeaderMap).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            this.CopySourceObject.MatchETag = cpArgs.CopySourceObject.MatchETag;
            this.CopySourceObject.ModifiedSince = cpArgs.CopySourceObject.ModifiedSince;
            this.CopySourceObject.NotMatchETag = cpArgs.CopySourceObject.NotMatchETag;
            this.CopySourceObject.UnModifiedSince = cpArgs.CopySourceObject.UnModifiedSince;
            this.CopySourceObject.HeaderMap = this.CopySourceObject.HeaderMap ?? new Dictionary<string, string>();
            this.CopySourceObject.HeaderMap = this.CopySourceObject.HeaderMap.Concat(cpArgs.CopySourceObject.HeaderMap).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            this.CopySourceObject.CopyOperationConditions = cpArgs.CopySourceObject.CopyOperationConditions?.Clone();
           
            this.BucketName = cpArgs.BucketName;
            this.ObjectName = cpArgs.ObjectName;

            this.HeaderMap = cpArgs.HeaderMap;
            this.SSE = cpArgs.SSE;
            this.SSE?.Marshal(this.SSEHeaders);

            this.HeaderMap = this.HeaderMap ?? new Dictionary<string, string>();
            this.HeaderMap = this.HeaderMap.Concat(cpArgs.HeaderMap).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            this.VersionId = cpArgs.VersionId;
            this.CopySourceObjectInfo = cpArgs.CopySourceObjectInfo;
            if (this.CopySourceObjectInfo.MetaData != null && this.CopySourceObjectInfo.MetaData.Count > 0)
            {
                this.HeaderMap = this.HeaderMap ?? new Dictionary<string, string>();
                this.HeaderMap = this.CopySourceObject.HeaderMap.Concat(cpArgs.CopySourceObject.HeaderMap).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
        }

        internal MultipartCopyUploadArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        internal MultipartCopyUploadArgs WithCopySize(long copySize)
        {
            this.CopySize = copySize;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            return request;
        }

    }
}