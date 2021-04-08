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
using System.IO;
using RestSharp;
using System.Globalization;
using System.Xml.Linq;
using System.Xml;
using System.Linq;

using Minio.DataModel;
using Minio.Exceptions;
using Minio.Helper;
using System.Security.Cryptography;

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

        internal override void Validate()
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
        internal override RestRequest BuildRequest(RestRequest request)
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

        internal override RestRequest BuildRequest(RestRequest request)
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

        internal override void Validate()
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

    public class StatObjectArgs : ObjectConditionalQueryArgs<StatObjectArgs>
    {
        internal long ObjectOffset { get; private set; }
        internal long ObjectLength { get; private set; }
        internal bool OffsetLengthSet { get; set; }

        public StatObjectArgs()
        {
            this.RequestMethod = Method.HEAD;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            if (!string.IsNullOrEmpty(this.VersionId))
            {
                request.AddQueryParameter("versionId",$"{this.VersionId}");
            }
            return request;
        }

        internal override void Validate()
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
            if (this.OffsetLengthSet)
            {
                if (this.ObjectOffset < 0 || this.ObjectLength < 0)
                {
                    throw new ArgumentException(nameof(this.ObjectOffset) + " and " + nameof(this.ObjectLength) + "cannot be less than 0.");
                }
                if (this.ObjectOffset == 0 && this.ObjectLength == 0)
                {
                    throw new ArgumentException("One of " + nameof(this.ObjectOffset) + " or " + nameof(this.ObjectLength) + "must be greater than 0.");
                }
            }
            this.Populate();
        }

        private void Populate()
        {
            this.Headers = new Dictionary<string, string>();
            if (this.SSE != null && this.SSE.GetType().Equals(EncryptionType.SSE_C))
            {
                this.SSE.Marshal(this.Headers);
            }
            if (OffsetLengthSet)
            {
                this.Headers["Range"] = "bytes=" + this.ObjectOffset.ToString() + "-" + (this.ObjectOffset + this.ObjectLength - 1).ToString();
            }
        }

        public StatObjectArgs WithOffsetAndLength(long offset, long length)
        {
            this.OffsetLengthSet = true;
            this.ObjectOffset = (offset < 0)?0:offset;
            this.ObjectLength = (length < 0)?0:length;
            return this;
        }

        public StatObjectArgs WithLength(long length)
        {
            this.OffsetLengthSet = true;
            this.ObjectOffset = 0;
            this.ObjectLength = (length < 0)?0:length;
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

        internal override RestRequest BuildRequest(RestRequest request)
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

        protected new void Validate()
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

        internal override void Validate()
        {
            base.Validate();
            if(string.IsNullOrEmpty(this.UploadId))
            {
                throw new InvalidOperationException(nameof(UploadId) + " cannot be empty. Please assign a valid upload ID to remove.");
            }
        }
        internal override RestRequest BuildRequest(RestRequest request)
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
        internal override RestRequest BuildRequest(RestRequest request)
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

        internal override RestRequest BuildRequest(RestRequest request)
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

    public class GetObjectArgs : ObjectConditionalQueryArgs<GetObjectArgs>
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

        internal override void Validate()
        {
            base.Validate();
            if (this.CallBack == null && string.IsNullOrEmpty(this.FileName))
            {
                throw new MinioException("Atleast one of " + nameof(this.CallBack) + ", CallBack method or " + nameof(this.FileName) + " file path to save need to be set for GetObject operation.");
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
            this.Populate();
        }

        private void Populate()
        {
            this.Headers = new Dictionary<string, string>();
            if (this.SSE != null && this.SSE.GetType().Equals(EncryptionType.SSE_C))
            {
                this.SSE.Marshal(this.Headers);
            }

            if (this.ObjectLength > 0 && this.ObjectOffset > 0)
            {
                this.Headers["Range"] = "bytes=" + this.ObjectOffset.ToString() + "-" + (this.ObjectOffset + this.ObjectLength - 1).ToString();
            }
            else if(this.ObjectLength == 0 && this.ObjectOffset > 0)
            {
                this.Headers["Range"] = "bytes=" + this.ObjectOffset.ToString() + "-";
            }
            else if(this.ObjectLength > 0 && this.ObjectOffset == 0)
            {
                this.Headers["Range"] = "bytes=-" + (this.ObjectLength - 1).ToString();
            }
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            if (!string.IsNullOrEmpty(this.VersionId))
            {
                request.AddQueryParameter("versionId",$"{this.VersionId}");
            }
            request.ResponseWriter = this.CallBack;

            return request;
        }   


        public GetObjectArgs WithCallbackStream(Action<Stream> cb)
        {
            this.CallBack = cb;
            return this;
        }

        public GetObjectArgs WithOffsetAndLength(long offset, long length)
        {
            this.OffsetLengthSet = true;
            this.ObjectOffset = (offset < 0)?0:offset;
            this.ObjectLength = (length < 0)?0:length;
            return this;
        }

        public GetObjectArgs WithLength(long length)
        {
            this.OffsetLengthSet = true;
            this.ObjectOffset = 0;
            this.ObjectLength = (length < 0)?0:length;
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

        internal override RestRequest BuildRequest(RestRequest request)
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

        internal override void Validate()
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

        internal override RestRequest BuildRequest(RestRequest request)
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
        internal Tagging ObjectTags { get; private set; }
        public SetObjectTagsArgs()
        {
            this.RequestMethod = Method.PUT;
        }


        public SetObjectTagsArgs WithTagging(Tagging tags)
        {
            this.ObjectTags = Tagging.GetObjectTags(tags.GetTags());
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
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

        internal override void Validate()
        {
            base.Validate();
            if (this.ObjectTags == null || this.ObjectTags.GetTags().Count == 0)
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

        internal override RestRequest BuildRequest(RestRequest request)
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

        internal override RestRequest BuildRequest(RestRequest request)
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

        internal override void Validate()
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

        internal override RestRequest BuildRequest(RestRequest request)
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

        internal override RestRequest BuildRequest(RestRequest request)
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
        internal override RestRequest BuildRequest(RestRequest request)
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

    public class CopySourceObjectArgs : ObjectConditionalQueryArgs<CopySourceObjectArgs>
    {
        internal string CopySourceObjectPath { get; set; }
        internal CopyConditions CopyOperationConditions { get; set; }
        public CopySourceObjectArgs()
        {
            this.RequestMethod = Method.PUT;
            this.CopyOperationConditions = new CopyConditions();
            this.Headers = new Dictionary<string, string>();
        }

        internal override void Validate()
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


        internal CopyObjectRequestArgs()
        {
            this.RequestMethod = Method.PUT;
            this.Headers = new Dictionary<string, string>();
            this.CopyOperationObjectType = typeof(CopyObjectResult);
        }

        internal CopyObjectRequestArgs WithQueryMap(Dictionary<string, string> queryMap)
        {
            this.QueryMap = new Dictionary<string, string>(queryMap);
            return this;
        }

        internal CopyObjectRequestArgs WithPartCondition(CopyConditions partCondition)
        {
            this.CopyCondition = partCondition.Clone();
            this.Headers = this.Headers ?? new Dictionary<string, string>();
            this.Headers["x-amz-copy-source-range"] = "bytes=" + partCondition.byteRangeStart.ToString() + "-" + partCondition.byteRangeEnd.ToString();

            return this;
        }

        internal CopyObjectRequestArgs WithReplaceMetadataDirective(bool replace)
        {
            this.ReplaceMetadataDirective = replace;
            return this;
        }

        internal CopyObjectRequestArgs WithReplaceTagsDirective(bool replace)
        {
            this.ReplaceTagsDirective = replace;
            return this;
        }

        public CopyObjectRequestArgs WithCopyObjectSource(CopySourceObjectArgs cs)
        {
            if (cs == null)
            {
                throw new InvalidOperationException("The copy source object needed for copy operation is not initialized.");
            }

            this.SourceObject = this.SourceObject ?? new CopySourceObjectArgs();
            this.SourceObject.RequestMethod = Method.PUT;
            this.SourceObject.BucketName = cs.BucketName;
            this.SourceObject.ObjectName = cs.ObjectName;
            this.SourceObject.VersionId = cs.VersionId;
            this.SourceObject.SSE = cs.SSE;
            this.SourceObject.Headers = new Dictionary<string, string>(cs.Headers);
            this.SourceObject.MatchETag = cs.MatchETag;
            this.SourceObject.ModifiedSince = cs.ModifiedSince;
            this.SourceObject.NotMatchETag = cs.NotMatchETag;
            this.SourceObject.UnModifiedSince = cs.UnModifiedSince;
            this.SourceObject.CopySourceObjectPath = $"{cs.BucketName}/{utils.UrlEncode(cs.ObjectName)}";
            this.SourceObject.CopyOperationConditions = cs.CopyOperationConditions?.Clone();
            return this;
        }

        public CopyObjectRequestArgs WithSourceObjectInfo(ObjectStat stat)
        {
            this.SourceObjectInfo = stat;
            return this;
        }
        internal override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            string sourceObjectPath = this.SourceObject.BucketName + "/" + utils.UrlEncode(this.SourceObject.ObjectName);
            if(!string.IsNullOrEmpty(this.SourceObject.VersionId))
            {
                sourceObjectPath += "?versionId=" + this.SourceObject.VersionId;
            }
            // Set the object source
            request.AddHeader("x-amz-copy-source", sourceObjectPath);

            if (this.QueryMap != null)
            {
                foreach (var query in this.QueryMap)
                {
                    request.AddQueryParameter(query.Key,query.Value);
                }
            }

            if (this.SourceObject.CopyOperationConditions != null)
            {
                foreach (var item in this.SourceObject.CopyOperationConditions.GetConditions())
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
            string replaceDirective = "COPY";
            if (this.ReplaceMetadataDirective)
            {
                replaceDirective = "REPLACE";
            }
            request.AddOrUpdateParameter("x-amz-metadata-directive", replaceDirective, ParameterType.HttpHeader);
            if (!string.IsNullOrEmpty(this.StorageClass))
            {
                request.AddOrUpdateParameter("x-amz-storage-class", this.StorageClass, ParameterType.HttpHeader);
            }
            if (this.ObjectLockSet)
            {
                if (!this.RetentionUntilDate.Equals(default(DateTime)))
                {
                    request.AddOrUpdateParameter("x-amz-object-lock-retain-until-date", utils.To8601String(this.RetentionUntilDate), ParameterType.HttpHeader);
                }
                request.AddOrUpdateParameter("x-amz-object-lock-mode",
                                            (this.ObjectLockRetentionMode == RetentionMode.GOVERNANCE)?"GOVERNANCE":"COMPLIANCE",
                                            ParameterType.HttpHeader);
            }

            return request;
        }

        internal CopyObjectRequestArgs WithCopyOperationObjectType(Type cp)
        {
            this.CopyOperationObjectType = cp;
            return this;
        }

        public CopyObjectRequestArgs WithObjectLockMode(RetentionMode mode)
        {
            this.ObjectLockSet = true;
            this.ObjectLockRetentionMode = mode;
            return this;
        }

        public CopyObjectRequestArgs WithObjectLockRetentionDate(DateTime untilDate)
        {
            this.ObjectLockSet = true;
            this.RetentionUntilDate = new DateTime(untilDate.Year, untilDate.Month, untilDate.Day,
                                                    untilDate.Hour, untilDate.Minute, untilDate.Second);
            return this;
        }

        internal override void Validate()
        {
            utils.ValidateBucketName(this.BucketName);//Object name can be same as that of source.
            if (this.SourceObject == null)
            {
                throw new InvalidOperationException(nameof(this.SourceObject) + " has not been assigned.");
            }
            this.Populate();
        }

        internal void Populate()
        {
            this.ObjectName = string.IsNullOrEmpty(this.ObjectName)?this.SourceObject.ObjectName:this.ObjectName;
            // Opting for concat as Headers may have byte range info .etc.
            if (!this.ReplaceMetadataDirective && this.SourceObjectInfo.MetaData != null)
            {
                this.Headers = this.SourceObjectInfo.MetaData.Concat(this.Headers).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            else if (this.ReplaceMetadataDirective)
            {
                this.Headers = this.Headers ?? new Dictionary<string, string>();
            }
        }
    }

    public class CopyObjectArgs : ObjectWriteArgs<CopyObjectArgs>
    {
        internal CopySourceObjectArgs SourceObject { get; set; }
        internal ObjectStat SourceObjectInfo { get; set; }
        internal bool ReplaceTagsDirective { get; set; }
        internal bool ReplaceMetadataDirective { get; set; }
        internal string StorageClass { get; set; }
        internal RetentionMode ObjectLockRetentionMode { get; set; }
        internal DateTime RetentionUntilDate { get; set; }
        internal bool ObjectLockSet { get; set; }


        public CopyObjectArgs()
        {
            this.RequestMethod = Method.PUT;
            this.SourceObject = new CopySourceObjectArgs();
            this.ReplaceTagsDirective = false;
            this.ReplaceMetadataDirective = false;
            this.ObjectLockSet = false;
            this.RetentionUntilDate = default(DateTime);
        }

        internal override void Validate()
        {
            // We don't need to call base validate.
            // If object name is empty we default to source object name.
            utils.ValidateBucketName(this.BucketName);
            if (this.SourceObject == null)
            {
                throw new InvalidOperationException(nameof(this.SourceObject) + " has not been assigned. Please use " + nameof(this.WithCopyObjectSource));
            }
            if (this.SourceObjectInfo == null)
            {
                throw new InvalidOperationException("StatObject result for the copy source object needed to continue copy operation. Use " + nameof(WithCopyObjectSourceStats) + " to initialize StatObject result.");
            }
            if (!string.IsNullOrEmpty(this.NotMatchETag) && !string.IsNullOrEmpty(this.MatchETag))
            {
                throw new InvalidOperationException("Invalid to set both Etag match conditions " + nameof(this.NotMatchETag) + " and " + nameof(this.MatchETag));
            }
            if ((this.ModifiedSince != null && !this.ModifiedSince.Equals(default(DateTime))) &&
                    (this.ModifiedSince != null && !this.UnModifiedSince.Equals(default(DateTime))))
            {
                throw new InvalidOperationException("Invalid to set both modified date match conditions " + nameof(this.ModifiedSince) + " and " + nameof(this.UnModifiedSince));
            }
            this.Populate();
        }

        private void Populate()
        {
            if (string.IsNullOrEmpty(this.ObjectName))
            {
                this.ObjectName = this.SourceObject.ObjectName;
            }
            if (this.SSE != null && this.SSE.GetType().Equals(EncryptionType.SSE_C))
            {
                this.Headers = new Dictionary<string, string>();
                this.SSE.Marshal(this.Headers);
            }
            if (!this.ReplaceMetadataDirective)
            {
                // Check in copy conditions if replace metadata has been set
                bool copyReplaceMeta = (this.SourceObject.CopyOperationConditions != null )?this.SourceObject.CopyOperationConditions.HasReplaceMetadataDirective() : false;
                this.WithReplaceMetadataDirective(copyReplaceMeta);
            }
            if (!this.ReplaceMetadataDirective)
            {
                this.Headers = this.Headers ?? new Dictionary<string, string>();
                this.Headers = this.Headers.Concat(this.SourceObjectInfo.MetaData).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.Last().Value);
            }
            else if (this.ReplaceMetadataDirective)
            {
                this.Headers = this.Headers ?? new Dictionary<string, string>();
            }
            if (this.Headers != null)
            {
                List<Tuple<string, string>> newKVList = new List<Tuple<string, string>>();
                foreach (var item in this.Headers)
                {
                    var key = item.Key;
                    if (!OperationsUtil.IsSupportedHeader(item.Key) && !item.Key.StartsWith("x-amz-meta", StringComparison.OrdinalIgnoreCase) &&
                        !OperationsUtil.IsSSEHeader(key))
                    {
                        newKVList.Add(new Tuple<string, string>("x-amz-meta-" + key.ToLowerInvariant(), item.Value));
                    }
                }
                foreach (var item in newKVList)
                {
                    this.Headers[item.Item1] = item.Item2;
                }
            }
        }

        public CopyObjectArgs WithCopyObjectSource(CopySourceObjectArgs cs)
        {
            if (cs == null)
            {
                throw new InvalidOperationException("The copy source object needed for copy operation is not initialized.");
            }

            this.SourceObject.RequestMethod = Method.PUT;
            this.SourceObject = this.SourceObject ?? new CopySourceObjectArgs();
            this.SourceObject.BucketName = cs.BucketName;
            this.SourceObject.ObjectName = cs.ObjectName;
            this.SourceObject.VersionId = cs.VersionId;
            this.SourceObject.SSE = cs.SSE;
            this.SourceObject.Headers = cs.Headers;
            this.SourceObject.MatchETag = cs.MatchETag;
            this.SourceObject.ModifiedSince = cs.ModifiedSince;
            this.SourceObject.NotMatchETag = cs.NotMatchETag;
            this.SourceObject.UnModifiedSince = cs.UnModifiedSince;
            this.SourceObject.CopySourceObjectPath = $"{cs.BucketName}/{utils.UrlEncode(cs.ObjectName)}";
            this.SourceObject.CopyOperationConditions = cs.CopyOperationConditions?.Clone();
            return this;
        }

        public CopyObjectArgs WithReplaceTagsDirective(bool replace)
        {
            this.ReplaceTagsDirective = replace;
            return this;
        }

        public CopyObjectArgs WithReplaceMetadataDirective(bool replace)
        {
            this.ReplaceMetadataDirective = replace;
            return this;
        }

        public CopyObjectArgs WithObjectLockMode(RetentionMode mode)
        {
            this.ObjectLockSet = true;
            this.ObjectLockRetentionMode = mode;
            return this;
        }

        public CopyObjectArgs WithObjectLockRetentionDate(DateTime untilDate)
        {
            this.ObjectLockSet = true;
            this.RetentionUntilDate = new DateTime(untilDate.Year, untilDate.Month, untilDate.Day,
                                                    untilDate.Hour, untilDate.Minute, untilDate.Second);
            return this;
        }

        internal CopyObjectArgs WithCopyObjectSourceStats(ObjectStat info)
        {
            this.SourceObjectInfo = info;
            if (info.MetaData != null && !this.ReplaceMetadataDirective)
            {
                this.SourceObject.Headers = this.SourceObject.Headers ?? new Dictionary<string, string>();
                this.SourceObject.Headers = this.SourceObject.Headers.Concat(info.MetaData).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            return this;
        }

        internal CopyObjectArgs WithStorageClass(string storageClass)
        {
            this.StorageClass = storageClass;
            return this;
        }

        public CopyObjectArgs WithRetentionUntilDate(DateTime date)
        {
            this.ObjectLockSet = true;
            this.RetentionUntilDate = date;
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
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
            if (this.LegalHoldEnabled != null && this.LegalHoldEnabled.Value)
            {
                request.AddOrUpdateParameter("x-amz-object-lock-legal-hold", "ON", ParameterType.HttpHeader);
            }
            if (this.ObjectLockSet)
            {
                if (!this.RetentionUntilDate.Equals(default(DateTime)))
                {
                    request.AddOrUpdateParameter("x-amz-object-lock-retain-until-date", utils.To8601String(this.RetentionUntilDate), ParameterType.HttpHeader);
                }
                request.AddOrUpdateParameter("x-amz-object-lock-mode",
                                            (this.ObjectLockRetentionMode == RetentionMode.GOVERNANCE)?"GOVERNANCE":"COMPLIANCE",
                                            ParameterType.HttpHeader);
            }

            return request;
        }
    }

    internal class NewMultipartUploadArgs<T> : ObjectWriteArgs<T>
                    where T : NewMultipartUploadArgs<T>
    {
        internal RetentionMode ObjectLockRetentionMode { get; set; }
        internal DateTime RetentionUntilDate { get; set; }
        internal bool ObjectLockSet { get; set; }

        internal NewMultipartUploadArgs()
        {
            this.RequestMethod = Method.POST;
        }

        public NewMultipartUploadArgs<T> WithObjectLockMode(RetentionMode mode)
        {
            this.ObjectLockSet = true;
            this.ObjectLockRetentionMode = mode;
            return this;
        }

        public NewMultipartUploadArgs<T> WithObjectLockRetentionDate(DateTime untilDate)
        {
            this.ObjectLockSet = true;
            this.RetentionUntilDate = new DateTime(untilDate.Year, untilDate.Month, untilDate.Day,
                                                    untilDate.Hour, untilDate.Minute, untilDate.Second);
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request.AddQueryParameter("uploads","");
            if (this.ObjectLockSet)
            {
                if (!this.RetentionUntilDate.Equals(default(DateTime)))
                {
                    request.AddOrUpdateParameter("x-amz-object-lock-retain-until-date", utils.To8601String(this.RetentionUntilDate), ParameterType.HttpHeader);
                }
                request.AddOrUpdateParameter("x-amz-object-lock-mode",
                                            (this.ObjectLockRetentionMode == RetentionMode.GOVERNANCE)?"GOVERNANCE":"COMPLIANCE",
                                            ParameterType.HttpHeader);
            }
            return request;
        }
    }

    internal class NewMultipartUploadPutArgs : NewMultipartUploadArgs<NewMultipartUploadPutArgs>
    {
    }
    internal class MultipartCopyUploadArgs : ObjectWriteArgs<MultipartCopyUploadArgs>
    {
        internal CopySourceObjectArgs SourceObject { get; set; }
        internal ObjectStat SourceObjectInfo { get; set; }
        internal long CopySize { get; set; }
        internal bool ReplaceMetadataDirective { get; set; }
        internal bool ReplaceTagsDirective { get; set; }
        internal string StorageClass { get; set; }
        internal RetentionMode ObjectLockRetentionMode { get; set; }
        internal DateTime RetentionUntilDate { get; set; }
        internal bool ObjectLockSet { get; set; }


        internal MultipartCopyUploadArgs(CopyObjectArgs args)
        {
            if (args == null || args.SourceObject == null)
            {
                string message = (args == null)? $"The constructor of " + nameof(CopyObjectRequestArgs) + "initialized with arguments of CopyObjectArgs null." :
                                                    $"The constructor of " + nameof(CopyObjectRequestArgs) + "initialized with arguments of CopyObjectArgs type but with " + nameof(args.SourceObject) + " not initialized.";
                throw new InvalidOperationException(message);
            }
            this.RequestMethod = Method.PUT;

            this.SourceObject = new CopySourceObjectArgs();
            this.SourceObject.BucketName = args.SourceObject.BucketName;
            this.SourceObject.ObjectName = args.SourceObject.ObjectName;
            this.SourceObject.VersionId = args.SourceObject.VersionId;
            this.SourceObject.CopyOperationConditions = args.SourceObject.CopyOperationConditions.Clone();
            this.SourceObject.MatchETag = args.SourceObject.MatchETag;
            this.SourceObject.ModifiedSince = args.SourceObject.ModifiedSince;
            this.SourceObject.NotMatchETag = args.SourceObject.NotMatchETag;
            this.SourceObject.UnModifiedSince = args.SourceObject.UnModifiedSince;
           
            // Destination part.
            this.BucketName = args.BucketName;
            this.ObjectName = args.ObjectName ?? args.SourceObject.ObjectName;
            this.SSE = args.SSE;
            this.SSE?.Marshal(this.Headers);
            this.VersionId = args.VersionId;
            this.SourceObjectInfo = args.SourceObjectInfo;
            // Header part
            if (!args.ReplaceMetadataDirective)
            {
                this.Headers = new Dictionary<string, string>(args.SourceObjectInfo.MetaData);
            }
            else if (args.ReplaceMetadataDirective)
            {
                this.Headers = this.Headers ?? new Dictionary<string, string>();
            }
            if (this.Headers != null)
            {
                List<Tuple<string, string>> newKVList = new List<Tuple<string, string>>();
                foreach (var item in this.Headers)
                {
                    var key = item.Key;
                    if (!OperationsUtil.IsSupportedHeader(item.Key) && !item.Key.StartsWith("x-amz-meta", StringComparison.OrdinalIgnoreCase) &&
                        !OperationsUtil.IsSSEHeader(key))
                    {
                        newKVList.Add(new Tuple<string, string>("x-amz-meta-" + key.ToLowerInvariant(), item.Value));
                    }
                }
                foreach (var item in newKVList)
                {
                    this.Headers[item.Item1] = item.Item2;
                }
            }
            this.ReplaceTagsDirective = args.ReplaceTagsDirective;
            if (args.ReplaceTagsDirective && args.ObjectTags != null && args.ObjectTags.TaggingSet.Tag.Count > 0) // Tags of Source object
            {
                this.ObjectTags = Tagging.GetObjectTags(args.ObjectTags.GetTags());
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

        internal MultipartCopyUploadArgs WithStorageClass(string storageClass)
        {
            this.StorageClass = storageClass;
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            if (this.ObjectTags != null && this.ObjectTags.TaggingSet != null
                    && this.ObjectTags.TaggingSet.Tag.Count > 0)
            {
                request.AddOrUpdateParameter("x-amz-tagging", this.ObjectTags.GetTagString(), ParameterType.HttpHeader);
                request.AddOrUpdateParameter("x-amz-tagging-directive",
                            this.ReplaceTagsDirective?"REPLACE":"COPY",
                            ParameterType.HttpHeader);
            }
            string replaceDirective = "COPY";
            if (this.ReplaceMetadataDirective)
            {
                replaceDirective = "REPLACE";
            }
            request.AddOrUpdateParameter("x-amz-metadata-directive", replaceDirective, ParameterType.HttpHeader);
            if (!string.IsNullOrEmpty(this.StorageClass))
            {
                request.AddOrUpdateParameter("x-amz-storage-class", this.StorageClass, ParameterType.HttpHeader);
            }
            if (this.ObjectLockSet)
            {
                if (!this.RetentionUntilDate.Equals(default(DateTime)))
                {
                    request.AddOrUpdateParameter("x-amz-object-lock-retain-until-date", utils.To8601String(this.RetentionUntilDate), ParameterType.HttpHeader);
                }
                request.AddOrUpdateParameter("x-amz-object-lock-mode",
                                            (this.ObjectLockRetentionMode == RetentionMode.GOVERNANCE)?"GOVERNANCE":"COMPLIANCE",
                                            ParameterType.HttpHeader);
            }

            return request;
        }

        internal MultipartCopyUploadArgs WithReplaceMetadataDirective(bool replace)
        {
            this.ReplaceMetadataDirective = replace;
            return this;
        }
        internal MultipartCopyUploadArgs WithObjectLockMode(RetentionMode mode)
        {
            this.ObjectLockSet = true;
            this.ObjectLockRetentionMode = mode;
            return this;
        }

        internal MultipartCopyUploadArgs WithObjectLockRetentionDate(DateTime untilDate)
        {
            this.ObjectLockSet = true;
            this.RetentionUntilDate = new DateTime(untilDate.Year, untilDate.Month, untilDate.Day,
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
            if (this.SourceObjectInfo == null || this.SourceObject == null)
            {
                throw new InvalidOperationException(nameof(this.SourceObjectInfo) + " and " + nameof(this.SourceObject) + " need to be initialized for a NewMultipartUpload operation to work.");
            }
            this.Populate();
        }

        private void Populate()
        {
            //Concat as Headers may have byte range info .etc.
            if (!this.ReplaceMetadataDirective && this.SourceObjectInfo.MetaData != null && this.SourceObjectInfo.MetaData.Count > 0)
            {
                this.Headers = this.SourceObjectInfo.MetaData.Concat(this.Headers).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            else if (this.ReplaceMetadataDirective)
            {
                this.Headers = this.Headers ?? new Dictionary<string, string>();
            }
            if (this.Headers != null)
            {
                List<Tuple<string, string>> newKVList = new List<Tuple<string, string>>();
                foreach (var item in this.Headers)
                {
                    var key = item.Key;
                    if (!OperationsUtil.IsSupportedHeader(item.Key) && !item.Key.StartsWith("x-amz-meta", StringComparison.OrdinalIgnoreCase) &&
                        !OperationsUtil.IsSSEHeader(key))
                    {
                        newKVList.Add(new Tuple<string, string>("x-amz-meta-" + key.ToLowerInvariant(), item.Value));
                    }
                }
                foreach (var item in newKVList)
                {
                    this.Headers[item.Item1] = item.Item2;
                }
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
            this.StorageClass = storageClass;
            return this;
        }

        internal NewMultipartUploadCopyArgs WithReplaceMetadataDirective(bool replace)
        {
            this.ReplaceMetadataDirective = replace;
            return this;
        }

        internal NewMultipartUploadCopyArgs WithReplaceTagsDirective(bool replace)
        {
            this.ReplaceTagsDirective = replace;
            return this;
        }

        public NewMultipartUploadCopyArgs WithSourceObjectInfo(ObjectStat stat)
        {
            this.SourceObjectInfo = stat;
            return this;
        }
        public NewMultipartUploadCopyArgs WithCopyObjectSource(CopySourceObjectArgs cs)
        {
            if (cs == null)
            {
                throw new InvalidOperationException("The copy source object needed for copy operation is not initialized.");
            }

            this.SourceObject = this.SourceObject ?? new CopySourceObjectArgs();
            this.SourceObject.RequestMethod = Method.PUT;
            this.SourceObject.BucketName = cs.BucketName;
            this.SourceObject.ObjectName = cs.ObjectName;
            this.SourceObject.VersionId = cs.VersionId;
            this.SourceObject.SSE = cs.SSE;
            this.SourceObject.Headers = cs.Headers;
            this.SourceObject.MatchETag = cs.MatchETag;
            this.SourceObject.ModifiedSince = cs.ModifiedSince;
            this.SourceObject.NotMatchETag = cs.NotMatchETag;
            this.SourceObject.UnModifiedSince = cs.UnModifiedSince;
            this.SourceObject.CopySourceObjectPath = $"{cs.BucketName}/{utils.UrlEncode(cs.ObjectName)}";
            this.SourceObject.CopyOperationConditions = cs.CopyOperationConditions?.Clone();
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request.AddQueryParameter("uploads","");
            if (this.ObjectTags != null && this.ObjectTags.TaggingSet != null
                    && this.ObjectTags.TaggingSet.Tag.Count > 0)
            {
                request.AddOrUpdateParameter("x-amz-tagging", this.ObjectTags.GetTagString(), ParameterType.HttpHeader);
                request.AddOrUpdateParameter("x-amz-tagging-directive",
                            this.ReplaceTagsDirective?"REPLACE":"COPY",
                            ParameterType.HttpHeader);
            }
            string replaceDirective = "COPY";
            if (this.ReplaceMetadataDirective)
            {
                replaceDirective = "REPLACE";
            }
            request.AddOrUpdateParameter("x-amz-metadata-directive", replaceDirective, ParameterType.HttpHeader);
            if (!string.IsNullOrWhiteSpace(this.StorageClass))
            {
                request.AddOrUpdateParameter("x-amz-storage-class", this.StorageClass, ParameterType.HttpHeader);
            }
            if (this.ObjectLockSet)
            {
                if (!this.RetentionUntilDate.Equals(default(DateTime)))
                {
                    request.AddOrUpdateParameter("x-amz-object-lock-retain-until-date", utils.To8601String(this.RetentionUntilDate), ParameterType.HttpHeader);
                }
                request.AddOrUpdateParameter("x-amz-object-lock-mode",
                                            (this.ObjectLockRetentionMode == RetentionMode.GOVERNANCE)?"GOVERNANCE":"COMPLIANCE",
                                            ParameterType.HttpHeader);
            }


            return request;
        }
    }

    internal class CompleteMultipartUploadArgs: ObjectWriteArgs<CompleteMultipartUploadArgs>
    {
        internal string UploadId { get; set; }
        internal Dictionary<int, string> ETags { get; set; }

        internal CompleteMultipartUploadArgs()
        {
            this.RequestMethod = Method.POST;
        }

        internal override void Validate()
        {
            base.Validate();
            if (string.IsNullOrWhiteSpace(this.UploadId))
            {
                throw new ArgumentNullException(nameof(this.UploadId) + " cannot be empty.");
            }
            if (this.ETags == null || this.ETags.Count <= 0)
            {
                throw new InvalidOperationException(nameof(this.ETags) + " dictionary cannot be empty.");
            }
        }

        internal CompleteMultipartUploadArgs(MultipartCopyUploadArgs args)
        {
            // destBucketName, destObjectName, metadata, sseHeaders
            this.RequestMethod = Method.POST;
            this.BucketName = args.BucketName;
            this.ObjectName = args.ObjectName ?? args.SourceObject.ObjectName;
            this.Headers = new Dictionary<string, string>();
            this.SSE = args.SSE;
            this.SSE?.Marshal(args.Headers);
            if (args.Headers != null && args.Headers.Count > 0)
            {
                this.Headers = this.Headers.Concat(args.Headers).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
        }

        internal CompleteMultipartUploadArgs WithUploadId(string uploadId)
        {
            this.UploadId = uploadId;
            return this;
        }

        internal CompleteMultipartUploadArgs WithETags(Dictionary<int, string> etags)
        {
            if (etags != null && etags.Count > 0)
            {
                this.ETags = new Dictionary<int, string>(etags);
            }
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request.AddQueryParameter("uploadId",$"{this.UploadId}");
            List<XElement> parts = new List<XElement>();

            for (int i = 1; i <= this.ETags.Count; i++)
            {
                parts.Add(new XElement("Part",
                                       new XElement("PartNumber", i),
                                       new XElement("ETag", this.ETags[i])));
            }
            var completeMultipartUploadXml = new XElement("CompleteMultipartUpload", parts);
            var bodyString = completeMultipartUploadXml.ToString();
            var body = System.Text.Encoding.UTF8.GetBytes(bodyString);
            request.AddParameter("application/xml", body, ParameterType.RequestBody);
            return request;
        }
    }

    internal class PutObjectPartArgs : PutObjectArgs
    {
        public PutObjectPartArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        internal override void Validate()
        {
            base.Validate();
            if (string.IsNullOrWhiteSpace(this.UploadId))
            {
                throw new ArgumentNullException(nameof(UploadId) + " not assigned for PutObjectPart operation.");
            }
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

        public new PutObjectPartArgs WithRequestBody(object data)
        {
            return (PutObjectPartArgs)base.WithRequestBody(data);
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
    }

    public class PutObjectArgs : ObjectWriteArgs<PutObjectArgs>
    {
        internal string UploadId { get; private set; }
        internal int PartNumber { get; set; }
        internal string FileName { get; set; }
        internal long ObjectSize { get; set; }
        internal Stream ObjectStreamData { get; set; }

        public PutObjectArgs()
        {
            this.RequestMethod = Method.PUT;
            this.RequestBody = null;
            this.ObjectStreamData = null;
            this.PartNumber = 0;
        }

        internal PutObjectArgs(PutObjectPartArgs args)
        {
            this.RequestMethod = Method.PUT;
            this.BucketName = args.BucketName;
            this.ContentType = args.ContentType ?? "application/octet-stream";
            this.FileName = args.FileName;
            this.Headers = args.Headers;
            this.ObjectName = args.ObjectName;
            this.ObjectSize = args.ObjectSize;
            this.PartNumber = args.PartNumber;
            this.SSE = args.SSE;
            this.UploadId = args.UploadId;
        }

        internal override void Validate()
        {
            base.Validate();
            if (this.RequestBody == null && this.ObjectStreamData == null && string.IsNullOrWhiteSpace(this.FileName))
            {
                throw new ArgumentNullException("Invalid input. " + nameof(RequestBody) + ", " + nameof(FileName) + " and " + nameof(ObjectStreamData) + " cannot be empty.");
            }
            if (this.PartNumber < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(PartNumber), this.PartNumber, "Invalid Part number value. Cannot be less than 0");
            }
            // Check if only one of filename or stream are initialized
            if (!string.IsNullOrWhiteSpace(this.FileName) && this.ObjectStreamData != null)
            {
                throw new ArgumentException("Only one of " + nameof(FileName) + " or " + nameof(ObjectStreamData) + " should be set.");
            }
            // Check atleast one of filename or stream are initialized
            if (string.IsNullOrWhiteSpace(this.FileName) && this.ObjectStreamData == null)
            {
                throw new ArgumentException("One of " + nameof(FileName) + " or " + nameof(ObjectStreamData) + " must be set.");
            }
            if (!string.IsNullOrWhiteSpace(this.FileName))
            {
                utils.ValidateFile(this.FileName);
            }
            this.Populate();
        }

        private void Populate()
        {
            if (!string.IsNullOrWhiteSpace(this.FileName))
            {
                FileInfo fileInfo = new FileInfo(this.FileName);
                this.ObjectSize = fileInfo.Length;
                this.ObjectStreamData = new FileStream(this.FileName, FileMode.Open, FileAccess.Read);
            }
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            if (string.IsNullOrWhiteSpace(this.ContentType))
            {
                this.ContentType = "application/octet-stream";
            }
            if (!this.Headers.ContainsKey("Content-Type"))
            {
                this.Headers["Content-Type"] = this.ContentType;
            }
            if (!string.IsNullOrWhiteSpace(this.UploadId) && this.PartNumber > 0)
            {
                request.AddQueryParameter("uploadId",$"{this.UploadId}");
                request.AddQueryParameter("partNumber",$"{this.PartNumber}");
            }
            if (this.ObjectTags != null && this.ObjectTags.TaggingSet != null
                    && this.ObjectTags.TaggingSet.Tag.Count > 0)
            {
                request.AddOrUpdateParameter("x-amz-tagging", this.ObjectTags.GetTagString(), ParameterType.HttpHeader);
            }
            if (this.Retention != null)
            {
                request.AddOrUpdateParameter("x-amz-object-lock-retain-until-date", this.Retention.RetainUntilDate, ParameterType.HttpHeader);
            }
            if (this.LegalHoldEnabled != null)
            {
                request.AddOrUpdateParameter("x-amz-object-lock-legal-hold", 
                    ((this.LegalHoldEnabled == true)?"ON":"OFF"),
                    ParameterType.HttpHeader);
            }
            if (this.RequestBody != null)
            {
                var sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash((byte[])this.RequestBody);
                string hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                request.AddOrUpdateParameter("x-amz-content-sha256", hex, ParameterType.HttpHeader);
            }

            return request;
        }
 
        public new PutObjectArgs WithHeaders(Dictionary<string, string> metaData)
        {
            var sseHeaders = new Dictionary<string, string>();
            this.Headers = this.Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (metaData != null) {
                foreach (KeyValuePair<string, string> p in metaData)
                {
                    var key = p.Key;
                    if (!OperationsUtil.IsSupportedHeader(p.Key) && !p.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase) &&
                        !OperationsUtil.IsSSEHeader(p.Key))
                    {
                        key = "x-amz-meta-" + key.ToLowerInvariant();
                    }
                    this.Headers[key] = p.Value;
                }
            }
            if (string.IsNullOrWhiteSpace(this.ContentType))
            {
                this.ContentType = "application/octet-stream";
            }
            if (!this.Headers.ContainsKey("Content-Type"))
            {
                this.Headers["Content-Type"] = this.ContentType;
            }
            return this;
        }

        internal PutObjectArgs WithUploadId(string id = null)
        {
            this.UploadId = id;
            return this;
        }

        internal PutObjectArgs WithPartNumber(int num)
        {
            this.PartNumber = num;
            return this;
        }

        public PutObjectArgs WithFileName(string file)
        {
            this.FileName = file;
            return this;
        }

        public PutObjectArgs WithObjectSize(long size)
        {
            this.ObjectSize = size;
            return this;
        }

        public PutObjectArgs WithStreamData(Stream data)
        {
            this.ObjectStreamData = data;
            return this;
        }

        ~PutObjectArgs()
        {
            if (!string.IsNullOrWhiteSpace(this.FileName) && this.ObjectStreamData != null)
            {
                ((FileStream)this.ObjectStreamData).Close();
            } else if (this.ObjectStreamData != null)
            {
                this.ObjectStreamData.Close();
            }
        }
   }
}