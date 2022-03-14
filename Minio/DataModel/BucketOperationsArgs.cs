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
using Minio.DataModel;
using Minio.DataModel.ILM;
using Minio.DataModel.Replication;
using Minio.DataModel.Tags;
using Minio.DataModel.ObjectLock;
using Minio.Exceptions;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Minio
{
    public class BucketExistsArgs : BucketArgs<BucketExistsArgs>
    {
        public BucketExistsArgs()
        {
            this.RequestMethod = HttpMethod.Head;
        }
    }

    public class RemoveBucketArgs : BucketArgs<RemoveBucketArgs>
    {
        public RemoveBucketArgs()
        {
            this.RequestMethod = HttpMethod.Delete;
        }
    }

    public class MakeBucketArgs : BucketArgs<MakeBucketArgs>
    {
        internal string Location { get; set; }
        internal bool ObjectLock { get; set; }
        public MakeBucketArgs()
        {
            this.RequestMethod = HttpMethod.Put;
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

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {
            // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
            if (!string.IsNullOrEmpty(this.Location) && this.Location != "us-east-1")
            {
                CreateBucketConfiguration config = new CreateBucketConfiguration(this.Location);
                string body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
                requestMessageBuilder.AddXmlBody(body);
                requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
                                utils.getMD5SumStr(Encoding.UTF8.GetBytes(body)));
            }
            if (this.ObjectLock)
            {
                requestMessageBuilder.AddOrUpdateHeaderParameter("X-Amz-Bucket-Object-Lock-Enabled", "true");
            }
            return requestMessageBuilder;
        }
    }
    public class ListObjectsArgs : BucketArgs<ListObjectsArgs>
    {
        internal string Prefix { get; private set; }
        internal bool Recursive { get; private set; }
        internal bool Versions { get; private set; }
        internal bool UseV2 { get; private set; }

        public ListObjectsArgs()
        {
            this.UseV2 = true;
            this.Versions = false;
        }

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

        public ListObjectsArgs WithListObjectsV1(bool useV1)
        {
            this.UseV2 = !useV1;
            return this;
        }
    }

    internal class GetObjectListArgs : BucketArgs<GetObjectListArgs>
    {
        internal string Delimiter { get; private set; }
        internal string Prefix { get; private set; }
        internal bool UseV2 { get; private set; }
        internal string Marker { get; private set; }
        internal string VersionIdMarker { get; private set; }
        internal bool Versions { get; private set; }
        internal string ContinuationToken { get; set; }

        public GetObjectListArgs()
        {
            this.RequestMethod = HttpMethod.Get;
            // Avoiding null values. Default is empty strings.
            this.Delimiter = string.Empty;
            this.Prefix = string.Empty;
            this.UseV2 = true;
            this.Versions = false;
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
            this.Marker = string.IsNullOrWhiteSpace(marker) ? string.Empty : marker;
            return this;
        }

        public GetObjectListArgs WithVersionIdMarker(string marker)
        {
            this.VersionIdMarker = string.IsNullOrWhiteSpace(marker) ? string.Empty : marker;
            return this;
        }

        public GetObjectListArgs WithVersions(bool versions)
        {
            this.Versions = versions;
            return this;
        }

        public GetObjectListArgs WithContinuationToken(string token)
        {
            this.ContinuationToken = string.IsNullOrWhiteSpace(token) ? string.Empty : token;
            return this;
        }

        public GetObjectListArgs WithListObjectsV1(bool useV1)
        {
            this.UseV2 = !useV1;
            return this;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {
            // using System.Web; Not sure if we need to add query parameters like this vs. requestMessageBuilder.AddQueryParameter()
            // var query = HttpUtility.ParseQueryString(string.Empty);
            // query["foo"] = "bar<>&-baz";
            // query["bar"] = "bazinga";
            // string queryString = query.ToString();        {


            requestMessageBuilder.AddQueryParameter("delimiter", this.Delimiter);
            requestMessageBuilder.AddQueryParameter("max-keys", "1000");
            requestMessageBuilder.AddQueryParameter("encoding-type", "url");
            if (!string.IsNullOrWhiteSpace(this.Prefix))
            {
                requestMessageBuilder.AddQueryParameter("prefix", this.Prefix);
            }
            if (this.Versions)
            {
                requestMessageBuilder.AddQueryParameter("versions", "");
                if (!string.IsNullOrWhiteSpace(this.Marker))
                {
                    requestMessageBuilder.AddQueryParameter("key-marker", this.Marker);
                }
                if (!string.IsNullOrWhiteSpace(this.VersionIdMarker))
                {
                    requestMessageBuilder.AddQueryParameter("version-id-marker", this.VersionIdMarker);
                }
            }
            else if (!this.Versions && this.UseV2)
            {
                requestMessageBuilder.AddQueryParameter("list-type", "2");
                if (!string.IsNullOrWhiteSpace(this.Marker))
                {
                    requestMessageBuilder.AddQueryParameter("start-after", this.Marker);
                }
                if (!string.IsNullOrWhiteSpace(this.ContinuationToken))
                {
                    requestMessageBuilder.AddQueryParameter("continuation-token", this.ContinuationToken);
                }
            }
            else if (!this.Versions && !this.UseV2)
            {
                requestMessageBuilder.AddQueryParameter("marker", this.Marker);
            }
            else
            {
                throw new InvalidOperationException("Wrong set of properties set.");
            }
            return requestMessageBuilder;
        }
    }

    public class GetPolicyArgs : BucketArgs<GetPolicyArgs>
    {
        public GetPolicyArgs()
        {
            this.RequestMethod = HttpMethod.Get;
        }
        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("policy", "");
            return requestMessageBuilder;
        }
    }

    public class SetPolicyArgs : BucketArgs<SetPolicyArgs>
    {
        internal string PolicyJsonString { get; private set; }
        public SetPolicyArgs()
        {
            this.RequestMethod = HttpMethod.Put;
        }
        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {
            if (string.IsNullOrEmpty(this.PolicyJsonString))
            {
                new MinioException("SetPolicyArgs needs the policy to be set to the right JSON contents.");
            }

            requestMessageBuilder.AddQueryParameter("policy", "");
            requestMessageBuilder.AddJsonBody(this.PolicyJsonString);
            return requestMessageBuilder;
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
            this.RequestMethod = HttpMethod.Delete;
        }
        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("policy", "");
            return requestMessageBuilder;
        }
    }

    public class GetBucketNotificationsArgs : BucketArgs<GetBucketNotificationsArgs>
    {
        public GetBucketNotificationsArgs()
        {
            this.RequestMethod = HttpMethod.Get;
        }
        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("notification", "");
            return requestMessageBuilder;
        }
    }
    public class SetBucketNotificationsArgs : BucketArgs<SetBucketNotificationsArgs>
    {
        internal BucketNotification BucketNotificationConfiguration { private set; get; }
        public SetBucketNotificationsArgs()
        {
            this.RequestMethod = HttpMethod.Put;
        }
        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {
            if (this.BucketNotificationConfiguration == null)
            {
                throw new UnexpectedMinioException("Cannot BuildRequest for SetBucketNotificationsArgs. BucketNotification configuration not assigned");
            }
            requestMessageBuilder.AddQueryParameter("notification", "");
            string body = utils.MarshalXML(BucketNotificationConfiguration, "http://s3.amazonaws.com/doc/2006-03-01/");
            // Convert string to a byte array
            byte[] bodyInBytes = Encoding.ASCII.GetBytes(body);
            requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
            requestMessageBuilder.SetBody(bodyInBytes);

            return requestMessageBuilder;
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
            this.RequestMethod = HttpMethod.Put;
        }
        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("notification", "");
            BucketNotification bucketNotificationConfiguration = new BucketNotification();
            string body = utils.MarshalXML(bucketNotificationConfiguration, "http://s3.amazonaws.com/doc/2006-03-01/");
            // Convert string to a byte array
            byte[] bodyInBytes = Encoding.ASCII.GetBytes(body);
            requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
            requestMessageBuilder.SetBody(bodyInBytes);

            return requestMessageBuilder;
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
            this.RequestMethod = HttpMethod.Get;
            this.EnableTrace = false;
            this.Events = new List<EventType>();
            this.Prefix = "";
            this.Suffix = "";
        }

        public override string ToString()
        {
            string str = string.Join("\n", new string[]{
            string.Format("\nRequestMethod= {0}", this.RequestMethod),
            string.Format("EnableTrace= {0}", this.EnableTrace)});

            var eventsAsStr = "";
            foreach (var eventType in this.Events)
            {
                if (!string.IsNullOrEmpty(eventsAsStr))
                    eventsAsStr += ", ";
                eventsAsStr += eventType.value;
            }
            return string.Join("\n", new string[]{str,
            string.Format("Events= [{0}]", eventsAsStr),
            string.Format("Prefix= {0}", this.Prefix),
            string.Format("Suffix= {0}\n", this.Suffix)});
        }

        public ListenBucketNotificationsArgs WithNotificationObserver(IObserver<MinioNotificationRaw> obs)
        {
            this.NotificationObserver = obs;
            return this;
        }

        internal readonly IEnumerable<ApiResponseErrorHandlingDelegate> NoErrorHandlers = Enumerable.Empty<ApiResponseErrorHandlingDelegate>();

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)

        {
            foreach (var eventType in this.Events)
            {
                requestMessageBuilder.AddQueryParameter("events", eventType.value);
            }
            requestMessageBuilder.AddQueryParameter("prefix", this.Prefix);
            requestMessageBuilder.AddQueryParameter("suffix", this.Suffix);

            requestMessageBuilder.ResponseWriter = async responseStream =>
            {
                using (responseStream)
                {
                    var sr = new StreamReader(responseStream);
                    while (!sr.EndOfStream)
                    {
                        try
                        {
                            string line = await sr.ReadLineAsync();
                            if (this.EnableTrace)
                            {
                                Console.WriteLine("== ListenBucketNotificationsAsync read line ==");
                                Console.WriteLine(line);
                                Console.WriteLine("==============================================");
                            }
                            string trimmed = line.Trim();
                            if (trimmed.Length > 2)
                            {
                                this.NotificationObserver.OnNext(new MinioNotificationRaw(trimmed));
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            };
            return requestMessageBuilder;
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
            foreach (EventType ev in events)
            {
            }

            return this;
        }
    }


    public class SetBucketEncryptionArgs : BucketArgs<SetBucketEncryptionArgs>
    {
        internal ServerSideEncryptionConfiguration EncryptionConfig { get; set; }

        public SetBucketEncryptionArgs()
        {
            this.RequestMethod = HttpMethod.Put;
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

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {
            if (this.EncryptionConfig == null)
            {
                this.EncryptionConfig = ServerSideEncryptionConfiguration.GetSSEConfigurationWithS3Rule();
            }

            requestMessageBuilder.AddQueryParameter("encryption", "");
            string body = utils.MarshalXML(this.EncryptionConfig, "http://s3.amazonaws.com/doc/2006-03-01/");
            // Convert string to a byte array
            byte[] bodyInBytes = Encoding.ASCII.GetBytes(body);
            requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
            requestMessageBuilder.SetBody(bodyInBytes);

            return requestMessageBuilder;
        }
    }

    public class GetBucketEncryptionArgs : BucketArgs<GetBucketEncryptionArgs>
    {
        public GetBucketEncryptionArgs()
        {
            this.RequestMethod = HttpMethod.Get;
        }
        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("encryption", "");
            return requestMessageBuilder;
        }
    }

    public class RemoveBucketEncryptionArgs : BucketArgs<RemoveBucketEncryptionArgs>
    {
        public RemoveBucketEncryptionArgs()
        {
            this.RequestMethod = HttpMethod.Delete;
        }
        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("encryption", "");
            return requestMessageBuilder;
        }
    }

    public class SetBucketTagsArgs : BucketArgs<SetBucketTagsArgs>
    {
        internal Tagging BucketTags { get; private set; }
        public SetBucketTagsArgs()
        {
            this.RequestMethod = HttpMethod.Put;
        }

        public SetBucketTagsArgs WithTagging(Tagging tags)
        {
            this.BucketTags = Tagging.GetObjectTags(tags.GetTags());
            return this;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("tagging", "");
            string body = this.BucketTags.MarshalXML();

            requestMessageBuilder.AddXmlBody(body);
            requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
                            utils.getMD5SumStr(Encoding.UTF8.GetBytes(body)));

            //
            return requestMessageBuilder;
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
            this.RequestMethod = HttpMethod.Get;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("tagging", "");
            return requestMessageBuilder;
        }
    }

    public class RemoveBucketTagsArgs : BucketArgs<RemoveBucketTagsArgs>
    {
        public RemoveBucketTagsArgs()
        {
            this.RequestMethod = HttpMethod.Delete;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("tagging", "");
            return requestMessageBuilder;
        }
    }

    public class SetObjectLockConfigurationArgs : BucketArgs<SetObjectLockConfigurationArgs>
    {
        public SetObjectLockConfigurationArgs()
        {
            this.RequestMethod = HttpMethod.Put;
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

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("object-lock", "");
            string body = utils.MarshalXML(this.LockConfiguration, "http://s3.amazonaws.com/doc/2006-03-01/");
            // Convert string to a byte array
            // byte[] bodyInBytes = Encoding.ASCII.GetBytes(body);

            // requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
            // requestMessageBuilder.SetBody(bodyInBytes);
            //
            // string body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
            requestMessageBuilder.AddXmlBody(body);
            requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
                            utils.getMD5SumStr(Encoding.UTF8.GetBytes(body)));
            //
            return requestMessageBuilder;
        }
    }

    public class GetObjectLockConfigurationArgs : BucketArgs<GetObjectLockConfigurationArgs>
    {
        public GetObjectLockConfigurationArgs()
        {
            this.RequestMethod = HttpMethod.Get;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("object-lock", "");
            return requestMessageBuilder;
        }
    }

    public class RemoveObjectLockConfigurationArgs : BucketArgs<RemoveObjectLockConfigurationArgs>
    {
        public RemoveObjectLockConfigurationArgs()
        {
            this.RequestMethod = HttpMethod.Put;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("object-lock", "");
            string body = utils.MarshalXML(new ObjectLockConfiguration(), "http://s3.amazonaws.com/doc/2006-03-01/");
            requestMessageBuilder.AddXmlBody(body);
            requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
                            utils.getMD5SumStr(Encoding.UTF8.GetBytes(body)));
            return requestMessageBuilder;
        }
    }

    public class SetBucketLifecycleArgs : BucketArgs<SetBucketLifecycleArgs>
    {
        internal LifecycleConfiguration BucketLifecycle { get; private set; }
        public SetBucketLifecycleArgs()
        {
            this.RequestMethod = HttpMethod.Put;
        }

        public SetBucketLifecycleArgs WithLifecycleConfiguration(LifecycleConfiguration lc)
        {
            this.BucketLifecycle = lc;
            return this;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("lifecycle", "");
            string body = this.BucketLifecycle.MarshalXML();
            // Convert string to a byte array
            byte[] bodyInBytes = Encoding.ASCII.GetBytes(body);
            requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
            requestMessageBuilder.SetBody(bodyInBytes);
            requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
                            utils.getMD5SumStr(bodyInBytes));

            return requestMessageBuilder;
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
            this.RequestMethod = HttpMethod.Get;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("lifecycle", "");
            return requestMessageBuilder;
        }
    }

    public class RemoveBucketLifecycleArgs : BucketArgs<RemoveBucketLifecycleArgs>
    {
        public RemoveBucketLifecycleArgs()
        {
            this.RequestMethod = HttpMethod.Delete;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("lifecycle", "");
            return requestMessageBuilder;
        }
    }

    public class GetBucketReplicationArgs : BucketArgs<GetBucketReplicationArgs>
    {
        public GetBucketReplicationArgs()
        {
            this.RequestMethod = HttpMethod.Get;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("replication", "");
            return requestMessageBuilder;
        }
    }

    public class SetBucketReplicationArgs : BucketArgs<SetBucketReplicationArgs>
    {
        internal ReplicationConfiguration BucketReplication { get; private set; }
        public SetBucketReplicationArgs()
        {
            this.RequestMethod = HttpMethod.Put;
        }

        public SetBucketReplicationArgs WithConfiguration(ReplicationConfiguration conf)
        {
            this.BucketReplication = conf;
            return this;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {
            requestMessageBuilder.AddQueryParameter("replication", "");
            string body = this.BucketReplication.MarshalXML();
            // Convert string to a byte array
            byte[] bodyInBytes = Encoding.ASCII.GetBytes(body);
            requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
            requestMessageBuilder.SetBody(bodyInBytes);

            return requestMessageBuilder;
        }
    }

    public class RemoveBucketReplicationArgs : BucketArgs<RemoveBucketReplicationArgs>
    {
        public RemoveBucketReplicationArgs()
        {
            this.RequestMethod = HttpMethod.Delete;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {

            requestMessageBuilder.AddQueryParameter("replication", "");
            return requestMessageBuilder;
        }
    }


}
