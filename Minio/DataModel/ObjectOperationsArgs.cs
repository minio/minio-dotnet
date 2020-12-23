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
using System.Security.Cryptography;
using System.Globalization;
using System.Xml;

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
}