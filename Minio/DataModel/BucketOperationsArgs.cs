/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020,2021 MinIO, Inc.
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
using Minio.DataModel;
using Minio.DataModel.ILM;
using Minio.DataModel.Replication;
using Minio.Exceptions;
using RestSharp;

namespace Minio
{
    public class BucketExistsArgs: BucketArgs<BucketExistsArgs>
    {
        public BucketExistsArgs()
        {
            this.RequestMethod = Method.HEAD;
        }
    }

    public class RemoveBucketArgs : BucketArgs<RemoveBucketArgs>
    {
        public RemoveBucketArgs()
        {
            this.RequestMethod = Method.DELETE;
        }
    }

    public class MakeBucketArgs : BucketArgs<MakeBucketArgs>
    {
        internal string Location { get; set; }
        internal bool ObjectLock { get; set; }
        public MakeBucketArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        public MakeBucketArgs WithLocation(string loc)
        {
            this.Location = loc;
            return this;
        }

        public MakeBucketArgs WithObjectLock()
        {
            this.ObjectLock = true;
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            request.RequestFormat = DataFormat.Xml;
            // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
            if (!string.IsNullOrEmpty(this.Location) && this.Location != "us-east-1")
            {
                CreateBucketConfiguration config = new CreateBucketConfiguration(this.Location);
                string body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
                request.AddParameter("text/xml", body, ParameterType.RequestBody);
            }
            if (this.ObjectLock)
            {
                request.AddOrUpdateParameter("X-Amz-Bucket-Object-Lock-Enabled", "true", ParameterType.HttpHeader);
            }
            return request;
        }
    }
    public class ListObjectsArgs : BucketArgs<ListObjectsArgs>
    {
        internal string Prefix;
        internal bool Recursive;
        internal bool Versions;
        public ListObjectsArgs WithPrefix(string prefix)
        {
            this.Prefix = prefix;
            return this;
        }
        public ListObjectsArgs WithRecursive(bool rec)
        {
            this.Recursive = rec;
            return this;
        }
        public ListObjectsArgs WithVersions(bool ver)
        {
            this.Versions = ver;
            return this;
        }
    }

    internal class GetObjectListArgs : BucketArgs<GetObjectListArgs>
    {
        internal string Delimiter { get; private set; }
        internal string Prefix { get; private set; }
        internal string Marker { get; private set; }
        internal bool Versions { get; private set; }
        internal string ContinuationToken { get; set; }

        public GetObjectListArgs()
        {
            this.RequestMethod = Method.GET;
            // Avoiding null values. Default is empty strings.
            this.Delimiter = string.Empty;
            this.Prefix = string.Empty;
            this.Marker = string.Empty;
        }

        public GetObjectListArgs WithDelimiter(string delim)
        {
            this.Delimiter = delim ?? string.Empty;
            return this;
        }

        public GetObjectListArgs WithPrefix(string prefix)
        {
            this.Prefix = prefix ?? string.Empty;
            return this;
        }

        public GetObjectListArgs WithMarker(string marker)
        {
            this.Marker = marker ?? string.Empty;
            return this;
        }

        public GetObjectListArgs WithVersions(bool versions)
        {
            this.Versions = versions;
            return this;
        }

        public GetObjectListArgs WithContinuationToken(string token)
        {
            this.ContinuationToken = string.IsNullOrWhiteSpace(token)?string.Empty:token;
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("list-type", "2");
            request.AddQueryParameter("delimiter",this.Delimiter);
            request.AddQueryParameter("prefix",this.Prefix);
            request.AddQueryParameter("max-keys", "1000");
            request.AddQueryParameter("encoding-type","url");
            if (!string.IsNullOrWhiteSpace(this.Marker))
            {
                request.AddQueryParameter("marker",this.Marker);
            }
            if (!string.IsNullOrWhiteSpace(this.ContinuationToken))
            {
                request.AddQueryParameter("continuation-token",this.ContinuationToken);
            }
            if (this.Versions)
            {
                request.AddQueryParameter("versions", "");
            }
            return request;
        }
    }

    public class GetPolicyArgs : BucketArgs<GetPolicyArgs>
    {
        public GetPolicyArgs()
        {
            this.RequestMethod = Method.GET;
        }
        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("policy","");
            return request;
        }
    }

    public class SetPolicyArgs : BucketArgs<SetPolicyArgs>
    {
        internal string PolicyJsonString { get; private set; }
        public SetPolicyArgs()
        {
            this.RequestMethod = Method.PUT;
        }
        internal override RestRequest BuildRequest(RestRequest request)
        {
            if (string.IsNullOrEmpty(this.PolicyJsonString))
            {
                new MinioException("SetPolicyArgs needs the policy to be set to the right JSON contents.");
            }
            request.AddQueryParameter("policy","");
            request.AddJsonBody(this.PolicyJsonString);
            return request;
        }
        public SetPolicyArgs WithPolicy(string policy)
        {
            this.PolicyJsonString = policy;
            return this;
        }
    }

    public class RemovePolicyArgs : BucketArgs<RemovePolicyArgs>
    {
        public RemovePolicyArgs()
        {
            this.RequestMethod = Method.DELETE;
        }
        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("policy","");
            return request;
        }
    }

    public class GetBucketNotificationsArgs : BucketArgs<GetBucketNotificationsArgs>
    {
        public GetBucketNotificationsArgs()
        {
            this.RequestMethod = Method.GET;
        }
        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("notification","");
            return request;
        }
    }
    public class SetBucketNotificationsArgs : BucketArgs<SetBucketNotificationsArgs>
    {
        internal BucketNotification BucketNotificationConfiguration { private set; get; }
        public SetBucketNotificationsArgs()
        {
            this.RequestMethod = Method.PUT;
        }
        internal override RestRequest BuildRequest(RestRequest request)
        {
            if (this.BucketNotificationConfiguration == null)
            {
                throw new UnexpectedMinioException("Cannot BuildRequest for SetBucketNotificationsArgs. BucketNotification configuration not assigned");
            }
            request.AddQueryParameter("notification","");
            string body = this.BucketNotificationConfiguration.ToXML();
            request.AddParameter("text/xml", body, ParameterType.RequestBody);
            return request;
        }
        public SetBucketNotificationsArgs WithBucketNotificationConfiguration(BucketNotification config)
        {
            this.BucketNotificationConfiguration = config;
            return this;
        }
    }

    public class RemoveAllBucketNotificationsArgs : BucketArgs<RemoveAllBucketNotificationsArgs>
    {
        public RemoveAllBucketNotificationsArgs()
        {
            this.RequestMethod = Method.PUT;
        }
        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("notification","");
            BucketNotification bucketNotificationConfiguration = new BucketNotification();
            string body = utils.MarshalXML(bucketNotificationConfiguration, "http://s3.amazonaws.com/doc/2006-03-01/");
            request.AddParameter("text/xml", body, ParameterType.RequestBody);

            return request;
        }
    }

    public class ListenBucketNotificationsArgs : BucketArgs<ListenBucketNotificationsArgs>
    {
        internal string Prefix { get; private set; }
        internal string Suffix { get; private set; }
        internal List<EventType> Events { get; private set; }
        internal IObserver<MinioNotificationRaw> NotificationObserver { get; private set; }
        public bool EnableTrace { get; private set; }

        public ListenBucketNotificationsArgs()
        {
            this.RequestMethod = Method.GET;
            this.EnableTrace = false;
            this.Events = new List<EventType>();
            this.Prefix="";
            this.Suffix="";
        }

        public ListenBucketNotificationsArgs WithNotificationObserver(IObserver<MinioNotificationRaw> obs)
        {
            this.NotificationObserver = obs;
            return this;
        }
        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("prefix",this.Prefix);
            request.AddQueryParameter("suffix",this.Suffix);
            foreach (var eventType in this.Events)
            {
                request.AddQueryParameter("events",eventType.value);
            }
            request.ResponseWriter = responseStream =>
            {
                using (responseStream)
                {
                    var sr = new StreamReader(responseStream);
                    while (true)
                    {
                        string line = sr.ReadLine();
                        if (this.EnableTrace)
                        {
                            Console.WriteLine("== ListenBucketNotificationsAsync read line ==");
                            Console.WriteLine(line);
                            Console.WriteLine("==============================================");
                        }
                        if (line == null)
                        {
                            break;
                        }
                        string trimmed = line.Trim();
                        if (trimmed.Length > 2)
                        {
                            this.NotificationObserver.OnNext(new MinioNotificationRaw(trimmed));
                        }
                    }
                }
            };

            return request;
        }

        internal ListenBucketNotificationsArgs WithEnableTrace(bool trace)
        {
            this.EnableTrace = trace;
            return this;
        }

        public ListenBucketNotificationsArgs WithPrefix(string prefix)
        {
            this.Prefix = prefix;
            return this;
        }

        public ListenBucketNotificationsArgs WithSuffix(string suffix)
        {
            this.Suffix = suffix;
            return this;
        }
    
        public ListenBucketNotificationsArgs WithEvents(List<EventType> events)
        {
            this.Events.AddRange(events);
            return this;
        }
    }


    public class SetBucketEncryptionArgs : BucketArgs<SetBucketEncryptionArgs>
    {
        internal ServerSideEncryptionConfiguration EncryptionConfig { get; set; }

        public SetBucketEncryptionArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        public SetBucketEncryptionArgs WithEncryptionConfig(ServerSideEncryptionConfiguration config)
        {
            this.EncryptionConfig = config;
            return this;
        }

        public SetBucketEncryptionArgs WithAESConfig()
        {
            this.EncryptionConfig = ServerSideEncryptionConfiguration.GetSSEConfigurationWithS3Rule();
            return this;
        }

        public SetBucketEncryptionArgs WithKMSConfig(string keyId = null)
        {
            this.EncryptionConfig = ServerSideEncryptionConfiguration.GetSSEConfigurationWithKMSRule(keyId);
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            if (this.EncryptionConfig == null)
            {
                this.EncryptionConfig = ServerSideEncryptionConfiguration.GetSSEConfigurationWithS3Rule();
            }
            request.AddQueryParameter("encryption","");
            string body = utils.MarshalXML(this.EncryptionConfig, "http://s3.amazonaws.com/doc/2006-03-01/");
            request.AddParameter(new Parameter("text/xml", body, ParameterType.RequestBody));

            return request;
        }
    }

    public class GetBucketEncryptionArgs : BucketArgs<GetBucketEncryptionArgs>
    {
        public GetBucketEncryptionArgs()
        {
            this.RequestMethod = Method.GET;
        }
        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("encryption","");
            return request;
        }
    }

    public class RemoveBucketEncryptionArgs : BucketArgs<RemoveBucketEncryptionArgs>
    {
        public RemoveBucketEncryptionArgs()
        {
            this.RequestMethod = Method.DELETE;
        }
        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("encryption","");
            return request;
        }
    }

    public class SetBucketTagsArgs : BucketArgs<SetBucketTagsArgs>
    {
        internal Tagging BucketTags { get; private set; }
        public SetBucketTagsArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        public SetBucketTagsArgs WithTagging(Tagging tags)
        {
            this.BucketTags = Tagging.GetObjectTags(tags.GetTags());
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("tagging","");
            string body = this.BucketTags.MarshalXML();
            request.AddParameter(new Parameter("text/xml", body, ParameterType.RequestBody));
            request.AddOrUpdateParameter("Content-MD5",
                                          utils.getMD5SumStr(System.Text.Encoding.UTF8.GetBytes(body)),
                                          ParameterType.HttpHeader);

            return request;
        }

        internal override void Validate()
        {
            base.Validate();
            if (this.BucketTags == null || this.BucketTags.GetTags().Count == 0)
            {
                throw new InvalidOperationException("Unable to set empty tags.");
            }
        }
    }

    public class GetBucketTagsArgs : BucketArgs<GetBucketTagsArgs>
    {
        public GetBucketTagsArgs()
        {
            this.RequestMethod = Method.GET;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("tagging","");
            return request;
        }
    }

    public class RemoveBucketTagsArgs : BucketArgs<RemoveBucketTagsArgs>
    {
        public RemoveBucketTagsArgs()
        {
            this.RequestMethod = Method.DELETE;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("tagging","");
            return request;
        }
    }

    public class SetObjectLockConfigurationArgs : BucketArgs<SetObjectLockConfigurationArgs>
    {
        public SetObjectLockConfigurationArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        internal ObjectLockConfiguration LockConfiguration { set; get; }
        public SetObjectLockConfigurationArgs WithLockConfiguration(ObjectLockConfiguration config)
        {
            this.LockConfiguration = config;
            return this;
        }

        internal override void Validate()
        {
            base.Validate();
            if (this.LockConfiguration == null)
            {
                throw new InvalidOperationException("The lock configuration object " + nameof(LockConfiguration) + " is not set. Please use " + nameof(WithLockConfiguration) + " to set.");
            }
        }
        
        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("object-lock","");
            string body = utils.MarshalXML(this.LockConfiguration, "http://s3.amazonaws.com/doc/2006-03-01/");
            request.AddParameter(new Parameter("text/xml", body, ParameterType.RequestBody));

            return request;
        }
    }

    public class GetObjectLockConfigurationArgs : BucketArgs<GetObjectLockConfigurationArgs>
    {
        public GetObjectLockConfigurationArgs()
        {
            this.RequestMethod = Method.GET;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("object-lock","");
            return request;
        }
    }

    public class RemoveObjectLockConfigurationArgs : BucketArgs<RemoveObjectLockConfigurationArgs>
    {
        public RemoveObjectLockConfigurationArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("object-lock","");
            string body = utils.MarshalXML(new ObjectLockConfiguration(), "http://s3.amazonaws.com/doc/2006-03-01/");
            request.AddParameter(new Parameter("text/xml", body, ParameterType.RequestBody));

            return request;
        }
    }

    public class SetBucketLifecycleArgs : BucketArgs<SetBucketLifecycleArgs>
    {
        internal LifecycleConfiguration BucketLifecycle { get; private set; }
        public SetBucketLifecycleArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        public SetBucketLifecycleArgs WithLifecycleConfiguration(LifecycleConfiguration lc)
        {
            this.BucketLifecycle = lc;
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("lifecycle","");
            string body = this.BucketLifecycle.MarshalXML();
            request.AddParameter(new Parameter("text/xml", body, ParameterType.RequestBody));
            request.AddOrUpdateParameter("Content-MD5",
                                          utils.getMD5SumStr(System.Text.Encoding.UTF8.GetBytes(body)),
                                          ParameterType.HttpHeader);
            return request;
        }

        internal override void Validate()
        {
            base.Validate();
            if (this.BucketLifecycle == null || this.BucketLifecycle.Rules.Count == 0)
            {
                throw new InvalidOperationException("Unable to set empty Lifecycle configuration.");
            }
        }
    }

    public class GetBucketLifecycleArgs : BucketArgs<GetBucketLifecycleArgs>
    {
        public GetBucketLifecycleArgs()
        {
            this.RequestMethod = Method.GET;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("lifecycle","");
            return request;
        }
    }

    public class RemoveBucketLifecycleArgs : BucketArgs<RemoveBucketLifecycleArgs>
    {
        public RemoveBucketLifecycleArgs()
        {
            this.RequestMethod = Method.DELETE;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("lifecycle","");
            return request;
        }
    }

    public class GetBucketReplicationArgs : BucketArgs<GetBucketReplicationArgs>
    {
        public GetBucketReplicationArgs()
        {
            this.RequestMethod = Method.GET;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("replication","");
            return request;
        }
    }

    public class SetBucketReplicationArgs : BucketArgs<SetBucketReplicationArgs>
    {
        internal ReplicationConfiguration BucketReplication { get; private set; }
        public SetBucketReplicationArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        public SetBucketReplicationArgs WithConfiguration(ReplicationConfiguration conf)
        {
            this.BucketReplication = conf;
            return this;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("replication","");
            string body = this.BucketReplication.MarshalXML();
            request.AddParameter(new Parameter("text/xml", body, ParameterType.RequestBody));
            request.AddOrUpdateParameter("Content-MD5",
                                          utils.getMD5SumStr(System.Text.Encoding.UTF8.GetBytes(body)),
                                          ParameterType.HttpHeader);

            return request;
        }
    }

    public class RemoveBucketReplicationArgs : BucketArgs<RemoveBucketReplicationArgs>
    {
        public RemoveBucketReplicationArgs()
        {
            this.RequestMethod = Method.DELETE;
        }

        internal override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("replication","");
            return request;
        }
    }
}