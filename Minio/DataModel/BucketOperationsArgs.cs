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
using System.Linq;
using System.Net.Http;
using System.Text;
using Minio.DataModel;
using Minio.DataModel.ILM;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Replication;
using Minio.DataModel.Tags;
using Minio.Exceptions;

namespace Minio;

public class BucketExistsArgs : BucketArgs<BucketExistsArgs>
{
    public BucketExistsArgs()
    {
        RequestMethod = HttpMethod.Head;
    }
}

public class RemoveBucketArgs : BucketArgs<RemoveBucketArgs>
{
    public RemoveBucketArgs()
    {
        RequestMethod = HttpMethod.Delete;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (Headers.ContainsKey(BucketForceDeleteKey))
            requestMessageBuilder.AddHeaderParameter(BucketForceDeleteKey, Headers[BucketForceDeleteKey]);
        return requestMessageBuilder;
    }
}

public class MakeBucketArgs : BucketArgs<MakeBucketArgs>
{
    public MakeBucketArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal string Location { get; set; }
    internal bool ObjectLock { get; set; }

    public MakeBucketArgs WithLocation(string loc)
    {
        Location = loc;
        return this;
    }

    public MakeBucketArgs WithObjectLock()
    {
        ObjectLock = true;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
        if (!string.IsNullOrEmpty(Location) && Location != "us-east-1")
        {
            var config = new CreateBucketConfiguration(Location);
            var body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
            requestMessageBuilder.AddXmlBody(body);
            requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
                utils.getMD5SumStr(Encoding.UTF8.GetBytes(body)));
        }

        if (ObjectLock) requestMessageBuilder.AddOrUpdateHeaderParameter("X-Amz-Bucket-Object-Lock-Enabled", "true");
        return requestMessageBuilder;
    }
}

public class ListObjectsArgs : BucketArgs<ListObjectsArgs>
{
    public ListObjectsArgs()
    {
        UseV2 = true;
        Versions = false;
    }

    internal string Prefix { get; private set; }
    internal bool Recursive { get; private set; }
    internal bool Versions { get; private set; }
    internal bool UseV2 { get; private set; }

    public ListObjectsArgs WithPrefix(string prefix)
    {
        Prefix = prefix;
        return this;
    }

    public ListObjectsArgs WithRecursive(bool rec)
    {
        Recursive = rec;
        return this;
    }

    public ListObjectsArgs WithVersions(bool ver)
    {
        Versions = ver;
        return this;
    }

    public ListObjectsArgs WithListObjectsV1(bool useV1)
    {
        UseV2 = !useV1;
        return this;
    }
}

internal class GetObjectListArgs : BucketArgs<GetObjectListArgs>
{
    public GetObjectListArgs()
    {
        RequestMethod = HttpMethod.Get;
        // Avoiding null values. Default is empty strings.
        Delimiter = string.Empty;
        Prefix = string.Empty;
        UseV2 = true;
        Versions = false;
        Marker = string.Empty;
    }

    internal string Delimiter { get; private set; }
    internal string Prefix { get; private set; }
    internal bool UseV2 { get; private set; }
    internal string Marker { get; private set; }
    internal string VersionIdMarker { get; private set; }
    internal bool Versions { get; private set; }
    internal string ContinuationToken { get; set; }

    public GetObjectListArgs WithDelimiter(string delim)
    {
        Delimiter = delim ?? string.Empty;
        return this;
    }

    public GetObjectListArgs WithPrefix(string prefix)
    {
        Prefix = prefix ?? string.Empty;
        return this;
    }

    public GetObjectListArgs WithMarker(string marker)
    {
        Marker = string.IsNullOrWhiteSpace(marker) ? string.Empty : marker;
        return this;
    }

    public GetObjectListArgs WithVersionIdMarker(string marker)
    {
        VersionIdMarker = string.IsNullOrWhiteSpace(marker) ? string.Empty : marker;
        return this;
    }

    public GetObjectListArgs WithVersions(bool versions)
    {
        Versions = versions;
        return this;
    }

    public GetObjectListArgs WithContinuationToken(string token)
    {
        ContinuationToken = string.IsNullOrWhiteSpace(token) ? string.Empty : token;
        return this;
    }

    public GetObjectListArgs WithListObjectsV1(bool useV1)
    {
        UseV2 = !useV1;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        foreach (var h in Headers)
            requestMessageBuilder.AddOrUpdateHeaderParameter(h.Key, h.Value);

        requestMessageBuilder.AddQueryParameter("delimiter", Delimiter);
        requestMessageBuilder.AddQueryParameter("max-keys", "1000");
        requestMessageBuilder.AddQueryParameter("encoding-type", "url");
        requestMessageBuilder.AddQueryParameter("prefix", Prefix);
        if (Versions)
        {
            requestMessageBuilder.AddQueryParameter("versions", "");
            if (!string.IsNullOrWhiteSpace(Marker)) requestMessageBuilder.AddQueryParameter("key-marker", Marker);
            if (!string.IsNullOrWhiteSpace(VersionIdMarker))
                requestMessageBuilder.AddQueryParameter("version-id-marker", VersionIdMarker);
        }
        else if (!Versions && UseV2)
        {
            requestMessageBuilder.AddQueryParameter("list-type", "2");
            if (!string.IsNullOrWhiteSpace(Marker)) requestMessageBuilder.AddQueryParameter("start-after", Marker);
            if (!string.IsNullOrWhiteSpace(ContinuationToken))
                requestMessageBuilder.AddQueryParameter("continuation-token", ContinuationToken);
        }
        else if (!Versions && !UseV2)
        {
            requestMessageBuilder.AddQueryParameter("marker", Marker);
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
        RequestMethod = HttpMethod.Get;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("policy", "");
        return requestMessageBuilder;
    }
}

public class SetPolicyArgs : BucketArgs<SetPolicyArgs>
{
    public SetPolicyArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal string PolicyJsonString { get; private set; }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (string.IsNullOrEmpty(PolicyJsonString))
            new MinioException("SetPolicyArgs needs the policy to be set to the right JSON contents.");

        requestMessageBuilder.AddQueryParameter("policy", "");
        requestMessageBuilder.AddJsonBody(PolicyJsonString);
        return requestMessageBuilder;
    }

    public SetPolicyArgs WithPolicy(string policy)
    {
        PolicyJsonString = policy;
        return this;
    }
}

public class RemovePolicyArgs : BucketArgs<RemovePolicyArgs>
{
    public RemovePolicyArgs()
    {
        RequestMethod = HttpMethod.Delete;
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
        RequestMethod = HttpMethod.Get;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("notification", "");
        return requestMessageBuilder;
    }
}

public class SetBucketNotificationsArgs : BucketArgs<SetBucketNotificationsArgs>
{
    public SetBucketNotificationsArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal BucketNotification BucketNotificationConfiguration { private set; get; }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (BucketNotificationConfiguration == null)
            throw new UnexpectedMinioException(
                "Cannot BuildRequest for SetBucketNotificationsArgs. BucketNotification configuration not assigned");
        requestMessageBuilder.AddQueryParameter("notification", "");
        var body = utils.MarshalXML(BucketNotificationConfiguration, "http://s3.amazonaws.com/doc/2006-03-01/");
        // Convert string to a byte array
        var bodyInBytes = Encoding.ASCII.GetBytes(body);
        requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
        requestMessageBuilder.SetBody(bodyInBytes);

        return requestMessageBuilder;
    }

    public SetBucketNotificationsArgs WithBucketNotificationConfiguration(BucketNotification config)
    {
        BucketNotificationConfiguration = config;
        return this;
    }
}

public class RemoveAllBucketNotificationsArgs : BucketArgs<RemoveAllBucketNotificationsArgs>
{
    public RemoveAllBucketNotificationsArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("notification", "");
        var bucketNotificationConfiguration = new BucketNotification();
        var body = utils.MarshalXML(bucketNotificationConfiguration, "http://s3.amazonaws.com/doc/2006-03-01/");
        // Convert string to a byte array
        var bodyInBytes = Encoding.ASCII.GetBytes(body);
        requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
        requestMessageBuilder.SetBody(bodyInBytes);

        return requestMessageBuilder;
    }
}

public class ListenBucketNotificationsArgs : BucketArgs<ListenBucketNotificationsArgs>
{
    internal readonly IEnumerable<ApiResponseErrorHandlingDelegate> NoErrorHandlers =
        Enumerable.Empty<ApiResponseErrorHandlingDelegate>();

    public ListenBucketNotificationsArgs()
    {
        RequestMethod = HttpMethod.Get;
        EnableTrace = false;
        Events = new List<EventType>();
        Prefix = "";
        Suffix = "";
    }

    internal string Prefix { get; private set; }
    internal string Suffix { get; private set; }
    internal List<EventType> Events { get; }
    internal IObserver<MinioNotificationRaw> NotificationObserver { get; private set; }
    public bool EnableTrace { get; private set; }

    public override string ToString()
    {
        var str = string.Join("\n", string.Format("\nRequestMethod= {0}", RequestMethod),
            string.Format("EnableTrace= {0}", EnableTrace));

        var eventsAsStr = "";
        foreach (var eventType in Events)
        {
            if (!string.IsNullOrEmpty(eventsAsStr))
                eventsAsStr += ", ";
            eventsAsStr += eventType.value;
        }

        return string.Join("\n", str, string.Format("Events= [{0}]", eventsAsStr), string.Format("Prefix= {0}", Prefix),
            string.Format("Suffix= {0}\n", Suffix));
    }

    public ListenBucketNotificationsArgs WithNotificationObserver(IObserver<MinioNotificationRaw> obs)
    {
        NotificationObserver = obs;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)

    {
        foreach (var eventType in Events) requestMessageBuilder.AddQueryParameter("events", eventType.value);
        requestMessageBuilder.AddQueryParameter("prefix", Prefix);
        requestMessageBuilder.AddQueryParameter("suffix", Suffix);

        requestMessageBuilder.FunctionResponseWriter = async (responseStream, cancellationToken) =>
        {
            using (responseStream)
            {
                var sr = new StreamReader(responseStream);
                while (!sr.EndOfStream)
                    try
                    {
                        var line = await sr.ReadLineAsync();
                        if (string.IsNullOrEmpty(line)) break;

                        if (EnableTrace)
                        {
                            Console.WriteLine("== ListenBucketNotificationsAsync read line ==");
                            Console.WriteLine(line);
                            Console.WriteLine("==============================================");
                        }

                        var trimmed = line.Trim();
                        if (trimmed.Length > 2) NotificationObserver.OnNext(new MinioNotificationRaw(trimmed));
                    }
                    catch
                    {
                        break;
                    }
            }
        };
        return requestMessageBuilder;
    }

    internal ListenBucketNotificationsArgs WithEnableTrace(bool trace)
    {
        EnableTrace = trace;
        return this;
    }

    public ListenBucketNotificationsArgs WithPrefix(string prefix)
    {
        Prefix = prefix;
        return this;
    }

    public ListenBucketNotificationsArgs WithSuffix(string suffix)
    {
        Suffix = suffix;
        return this;
    }

    public ListenBucketNotificationsArgs WithEvents(List<EventType> events)
    {
        Events.AddRange(events);
        foreach (var ev in events)
        {
        }

        return this;
    }
}

public class SetBucketEncryptionArgs : BucketArgs<SetBucketEncryptionArgs>
{
    public SetBucketEncryptionArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal ServerSideEncryptionConfiguration EncryptionConfig { get; set; }

    public SetBucketEncryptionArgs WithEncryptionConfig(ServerSideEncryptionConfiguration config)
    {
        EncryptionConfig = config;
        return this;
    }

    public SetBucketEncryptionArgs WithAESConfig()
    {
        EncryptionConfig = ServerSideEncryptionConfiguration.GetSSEConfigurationWithS3Rule();
        return this;
    }

    public SetBucketEncryptionArgs WithKMSConfig(string keyId = null)
    {
        EncryptionConfig = ServerSideEncryptionConfiguration.GetSSEConfigurationWithKMSRule(keyId);
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (EncryptionConfig == null)
            EncryptionConfig = ServerSideEncryptionConfiguration.GetSSEConfigurationWithS3Rule();

        requestMessageBuilder.AddQueryParameter("encryption", "");
        var body = utils.MarshalXML(EncryptionConfig, "http://s3.amazonaws.com/doc/2006-03-01/");
        // Convert string to a byte array
        var bodyInBytes = Encoding.ASCII.GetBytes(body);
        requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
        requestMessageBuilder.SetBody(bodyInBytes);

        return requestMessageBuilder;
    }
}

public class GetBucketEncryptionArgs : BucketArgs<GetBucketEncryptionArgs>
{
    public GetBucketEncryptionArgs()
    {
        RequestMethod = HttpMethod.Get;
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
        RequestMethod = HttpMethod.Delete;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("encryption", "");
        return requestMessageBuilder;
    }
}

public class SetBucketTagsArgs : BucketArgs<SetBucketTagsArgs>
{
    public SetBucketTagsArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal Tagging BucketTags { get; private set; }

    public SetBucketTagsArgs WithTagging(Tagging tags)
    {
        BucketTags = Tagging.GetBucketTags(tags.GetTags());
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("tagging", "");
        var body = BucketTags.MarshalXML();

        requestMessageBuilder.AddXmlBody(body);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            utils.getMD5SumStr(Encoding.UTF8.GetBytes(body)));

        //
        return requestMessageBuilder;
    }

    internal override void Validate()
    {
        base.Validate();
        if (BucketTags == null || BucketTags.GetTags().Count == 0)
            throw new InvalidOperationException("Unable to set empty tags.");
    }
}

public class GetBucketTagsArgs : BucketArgs<GetBucketTagsArgs>
{
    public GetBucketTagsArgs()
    {
        RequestMethod = HttpMethod.Get;
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
        RequestMethod = HttpMethod.Delete;
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
        RequestMethod = HttpMethod.Put;
    }

    internal ObjectLockConfiguration LockConfiguration { set; get; }

    public SetObjectLockConfigurationArgs WithLockConfiguration(ObjectLockConfiguration config)
    {
        LockConfiguration = config;
        return this;
    }

    internal override void Validate()
    {
        base.Validate();
        if (LockConfiguration == null)
            throw new InvalidOperationException("The lock configuration object " + nameof(LockConfiguration) +
                                                " is not set. Please use " + nameof(WithLockConfiguration) +
                                                " to set.");
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("object-lock", "");
        var body = utils.MarshalXML(LockConfiguration, "http://s3.amazonaws.com/doc/2006-03-01/");
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
        RequestMethod = HttpMethod.Get;
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
        RequestMethod = HttpMethod.Put;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("object-lock", "");
        var body = utils.MarshalXML(new ObjectLockConfiguration(), "http://s3.amazonaws.com/doc/2006-03-01/");
        requestMessageBuilder.AddXmlBody(body);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            utils.getMD5SumStr(Encoding.UTF8.GetBytes(body)));
        return requestMessageBuilder;
    }
}

public class SetBucketLifecycleArgs : BucketArgs<SetBucketLifecycleArgs>
{
    public SetBucketLifecycleArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal LifecycleConfiguration BucketLifecycle { get; private set; }

    public SetBucketLifecycleArgs WithLifecycleConfiguration(LifecycleConfiguration lc)
    {
        BucketLifecycle = lc;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("lifecycle", "");
        var body = BucketLifecycle.MarshalXML();
        // Convert string to a byte array
        var bodyInBytes = Encoding.ASCII.GetBytes(body);
        requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
        requestMessageBuilder.SetBody(bodyInBytes);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            utils.getMD5SumStr(bodyInBytes));

        return requestMessageBuilder;
    }

    internal override void Validate()
    {
        base.Validate();
        if (BucketLifecycle == null || BucketLifecycle.Rules.Count == 0)
            throw new InvalidOperationException("Unable to set empty Lifecycle configuration.");
    }
}

public class GetBucketLifecycleArgs : BucketArgs<GetBucketLifecycleArgs>
{
    public GetBucketLifecycleArgs()
    {
        RequestMethod = HttpMethod.Get;
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
        RequestMethod = HttpMethod.Delete;
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
        RequestMethod = HttpMethod.Get;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("replication", "");
        return requestMessageBuilder;
    }
}

public class SetBucketReplicationArgs : BucketArgs<SetBucketReplicationArgs>
{
    public SetBucketReplicationArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal ReplicationConfiguration BucketReplication { get; private set; }

    public SetBucketReplicationArgs WithConfiguration(ReplicationConfiguration conf)
    {
        BucketReplication = conf;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("replication", "");
        var body = BucketReplication.MarshalXML();
        // Convert string to a byte array
        var bodyInBytes = Encoding.ASCII.GetBytes(body);
        requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
        requestMessageBuilder.SetBody(bodyInBytes);

        return requestMessageBuilder;
    }
}

public class RemoveBucketReplicationArgs : BucketArgs<RemoveBucketReplicationArgs>
{
    public RemoveBucketReplicationArgs()
    {
        RequestMethod = HttpMethod.Delete;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("replication", "");
        return requestMessageBuilder;
    }
}