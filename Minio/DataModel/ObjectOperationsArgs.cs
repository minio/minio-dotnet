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
using RestSharp;
using System.Collections.Generic;
using System.Linq;

using Minio.DataModel;


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
                throw new ArgumentException(nameof(this.NotMatchETag) + " and " + nameof(this.MatchETag) + " being set is not allowed.");
            }
            if ((this.ModifiedSince != null && !this.ModifiedSince.Equals(default(DateTime))) &&
                    (this.ModifiedSince != null && !this.UnModifiedSince.Equals(default(DateTime))))
            {
                throw new ArgumentException(nameof(this.NotMatchETag) + " and " + nameof(this.MatchETag) + " being set is not allowed.");
            }
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

    internal class CopyObjectRequestArgs : ObjectVersionArgs<CopyObjectArgs>
    {
        internal CopySourceObjectArgs CopySourceObject { get; set; }
        internal ObjectStat CopySourceObjectInfo { get; set; }
        internal Type CopyOperationObjectType { get; set; }

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
            if (cpArgs.CopySourceObject.QueryParameters != null)
            {
                this.CopySourceObject.QueryParameters = new Dictionary<string, string>();
                this.CopySourceObject.QueryParameters = this.CopySourceObject.QueryParameters.Concat(cpArgs.CopySourceObject.QueryParameters).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            this.CopySourceObject.CopyOperationConditions = cpArgs.CopySourceObject.CopyOperationConditions?.Clone();
            this.HeaderMap = (cpArgs.HeaderMap != null)?new Dictionary<string, string>(cpArgs.HeaderMap):new Dictionary<string, string>();
            this.CopySourceObjectInfo = cpArgs.CopySourceObjectInfo;
            if (cpArgs.CopySourceObjectInfo.MetaData != null && cpArgs.CopySourceObjectInfo.MetaData.Count > 0)
            {
                this.HeaderMap = this.HeaderMap.Concat(cpArgs.CopySourceObjectInfo.MetaData).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            this.CopySourceObject.SSE = cpArgs.CopySourceObject.SSE;
            this.CopySourceObject.VersionId = cpArgs.CopySourceObject.VersionId;

            this.RequestMethod = Method.PUT;
            this.BucketName = cpArgs.BucketName;
            this.ObjectName = cpArgs.ObjectName;
            if (this.CopySourceObject.HeaderMap != null)
            {
                this.HeaderMap = this.HeaderMap.Concat(this.CopySourceObject.HeaderMap).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            this.QueryParameters = cpArgs.QueryParameters ?? new Dictionary<string, string>();
            if (this.CopySourceObject.QueryParameters != null)
            {
                this.QueryParameters = this.QueryParameters.Concat(this.CopySourceObject.QueryParameters).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }

            this.RequestBody = cpArgs.RequestBody;
            this.RequestMethod = Method.PUT;
            this.SSE = cpArgs.SSE;
            this.SSEHeaders = cpArgs.SSEHeaders ?? new Dictionary<string, string>();
            if (this.CopySourceObject.SSEHeaders != null)
            {
                this.SSEHeaders = this.SSEHeaders.Concat(this.CopySourceObject.SSEHeaders).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            this.VersionId = cpArgs.VersionId;
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
            this.QueryParameters = mcArgs.QueryParameters;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            string sourceObjectPath = this.CopySourceObject.BucketName + "/" + utils.UrlEncode(this.CopySourceObject.ObjectName);
            // Set the object source
            request.AddHeader("x-amz-copy-source", sourceObjectPath);

            if (this.QueryParameters != null)
            {
                foreach (var query in this.QueryParameters)
                {
                    request.AddQueryParameter(query.Key,query.Value);
                }
            }

            if (this.CopySourceObject.CopyOperationConditions != null)
            {
                foreach (var item in this.CopySourceObject.CopyOperationConditions.GetConditions())
                {
                    request.AddHeader(item.Key, item.Value);
                }
            }

            return request;
        }

        public CopyObjectRequestArgs WithCopyOperationObjectType(Type cp)
        {
            this.CopyOperationObjectType = cp;
            return this;
        }
    }

    public class CopyObjectArgs : ObjectVersionArgs<CopyObjectArgs>
    {
        internal CopySourceObjectArgs CopySourceObject { get; set; }
        internal ObjectStat CopySourceObjectInfo { get; set; }

        public CopyObjectArgs()
        {
            this.RequestMethod = Method.PUT;
            this.CopySourceObject = new CopySourceObjectArgs();
        }

        public override void Validate()
        {
            // We don't need base validate.
            // If object name is empty we default to source object name.
            this.CopySourceObject.Validate();
            utils.ValidateBucketName(this.BucketName);
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
            this.CopySourceObject.HeaderMap = cs.HeaderMap;
            this.CopySourceObject.MatchETag = cs.MatchETag;
            this.CopySourceObject.ModifiedSince = cs.ModifiedSince;
            this.CopySourceObject.NotMatchETag = cs.NotMatchETag;
            this.CopySourceObject.UnModifiedSince = cs.UnModifiedSince;
            this.CopySourceObject.CopySourceObjectPath = $"{cs.BucketName}/{utils.UrlEncode(cs.ObjectName)}";
            this.CopySourceObject.CopyOperationConditions = cs.CopyOperationConditions?.Clone(); 
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

        public CopyObjectArgs WithMatchETag(string etag)
        {
            this.CopySourceObject.WithMatchETag(etag);
            return this;
        }
        public CopyObjectArgs WithNotMatchETag(string etag)
        {
            this.CopySourceObject.WithNotMatchETag(etag);
            return this;
        }
        public CopyObjectArgs WithModifiedSince(DateTime d)
        {
            this.CopySourceObject.WithUnModifiedSince(d);
            return this;
        }
        public CopyObjectArgs WithUnModifiedSince(DateTime d)
        {
            this.CopySourceObject.WithUnModifiedSince(d);
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
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
            if (cpArgs.CopySourceObject.QueryParameters != null)
            {
                this.CopySourceObject.QueryParameters = new Dictionary<string, string>();
                this.CopySourceObject.QueryParameters = this.CopySourceObject.QueryParameters.Concat(cpArgs.CopySourceObject.QueryParameters).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }
            this.CopySourceObject.CopyOperationConditions = cpArgs.CopySourceObject.CopyOperationConditions?.Clone();
           
            this.BucketName = cpArgs.BucketName;
            this.ObjectName = cpArgs.ObjectName;

            this.HeaderMap = cpArgs.HeaderMap;
            this.SSE = cpArgs.SSE;
            if (this.SSE != null)
            {
                this.SSE.Marshal(this.SSEHeaders);
            }

            if (cpArgs.QueryParameters != null && cpArgs.QueryParameters.Count > 0)
            {
                this.QueryParameters = this.QueryParameters ?? new Dictionary<string, string>();
                this.QueryParameters = this.QueryParameters.Concat(cpArgs.QueryParameters).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value);
            }

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
    }
}