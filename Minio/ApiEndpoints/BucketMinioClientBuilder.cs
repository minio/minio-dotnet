/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2020 MinIO, Inc.
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

using Minio.DataModel;
using Minio.Exceptions;
using RestSharp;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Minio
{
    public partial class MinioClient
    {
        // MinioClient with Encryption parameters
        protected MinioClient(string bucket)
        {
            if ( this.BucketMinioClientArgs == null )
            {
                this.BucketMinioClientArgs = new BucketClientArgs();
            }
            this.BucketMinioClientArgs.BucketName = bucket;
            initDone = false;
        }

        protected MinioClient(string bucket, SSEC sse)
        {
            if ( this.EncryptionMinioClientArgs == null )
            {
                this.EncryptionMinioClientArgs = new EncryptionClientArgs();
            }
            this.EncryptionMinioClientArgs.SseConfiguration = sse;
            this.EncryptionMinioClientArgs.BktClientArgs.BucketName = bucket;
            initDone = false;
        }

        // MinioClient with Listen Bucket Notification args
        protected MinioClient(string bucket, string prefix, string suffix, string[] events)
        {
            if ( this.ListenNotificationMinioClientArgs == null )
            {
                this.ListenNotificationMinioClientArgs = new ListenBucketNotificationArgs();
            }
            this.ListenNotificationMinioClientArgs.BktClientArgs.BucketName = bucket;
            this.ListenNotificationMinioClientArgs.Prefix = prefix;
            this.ListenNotificationMinioClientArgs.Suffix = suffix;
            Array.Copy(events, this.ListenNotificationMinioClientArgs.Events, events.Length);
            initDone = false;
        }

        protected MinioClient(string bucket, bool enableVersioning, bool suspendVersioning, string location="us-east-1")
        {
            if ( this.BucketMinioClientArgs == null )
            {
                this.BucketMinioClientArgs = new BucketClientArgs();
            }
            this.BucketMinioClientArgs.BucketName = bucket;
            this.BucketMinioClientArgs.VersioningEnabled = enableVersioning;
            this.BucketMinioClientArgs.VersioningSuspended = suspendVersioning;
            initDone = false;
        }

        public static MinioClient NewClient(string bucket, string location="us-east-1")
        {
            var mc = new MinioClient();
            mc.BucketMinioClientArgs = new BucketClientArgs();
            mc.BucketMinioClientArgs.BucketName = bucket;
            mc.BucketMinioClientArgs.Location = location;
            return mc;
        }

        public static MinioClient NewClient(string bucket, SSEC sse)
        {
            return new MinioClient(bucket, sse);
        }

        // For Listen Bucket Notification
        public static MinioClient NewClient(string bucket, string prefix, string suffix, string[] events)
        {
            return new MinioClient(bucket, prefix, suffix, events);
        }

        // For bucket clients with versioning enabled/suspended
        public static MinioClient NewClient(string bucket, bool enableVersioning, bool suspendVersioning)
        {
            if ( suspendVersioning && !enableVersioning )
            {
                throw new InvalidMinioOperationException(bucket, "Versioning has to be enabled to be suspended.");
            }
            return new MinioClient(bucket, enableVersioning, suspendVersioning);
        }

        // For Listen Bucket Notification
        public MinioClient WithAffix(string prefix = "", string suffix="")
        {
            // TODO: Copy BucketMinioClientArgs. If not assigned, throw exception
            this.ListenNotificationMinioClientArgs.BktClientArgs.BucketName = this.BucketMinioClientArgs.BucketName;

            this.ListenNotificationMinioClientArgs.Prefix = prefix;
            this.ListenNotificationMinioClientArgs.Suffix = suffix;
            return this;
        }

        // For Listen Bucket Notification
        public MinioClient WithEvents(string[] events)
        {
            // TODO: Copy BucketMinioClientArgs. If not assigned, throw exception
            this.ListenNotificationMinioClientArgs.BktClientArgs.BucketName = this.BucketMinioClientArgs.BucketName;
            Array.Copy(events, this.ListenNotificationMinioClientArgs.Events, events.Length);
            return this;
        }

        public MinioClient WithVersioningEnabled()
        {
            // TODO: Check all necessary BucketMinioClientArgs is assigned. Throw exception otherwise.
            this.BucketMinioClientArgs.VersioningEnabled = true;
            this.BucketMinioClientArgs.VersioningSuspended = false;
            return this;
        }

        public MinioClient WithVersioningSuspended()
        {
            // TODO: Check all necessary BucketMinioClientArgs is assigned. Throw exception otherwise.
            this.BucketMinioClientArgs.VersioningEnabled = true;
            this.BucketMinioClientArgs.VersioningSuspended = true;
            return this;
        }

        private void CheckBucketArgs()
        {
            if ( this.MinioClientArgs == null && this.BucketMinioClientArgs == null )
            {
                throw new MinioException("Initialization error. Bucket Args not initialized.");
            }
            if ( this.MinioClientArgs != null )
            {
                this.BucketMinioClientArgs = new BucketClientArgs(this.MinioClientArgs);
                this.MinioClientArgs = null;
            }
        }

        private void CheckListenBucketNotificationArgs()
        {
            if ( this.MinioClientArgs == null && this.BucketMinioClientArgs == null )
            {
                throw new MinioException("Initialization error. Listen Notification Arguments not initialized.");
            }
            if ( this.MinioClientArgs != null )
            {
                this.ListenNotificationMinioClientArgs = new ListenBucketNotificationArgs(this.MinioClientArgs);
                this.ListenNotificationMinioClientArgs.BktClientArgs = new BucketClientArgs();
                this.MinioClientArgs = null;
            }
            if ( this.BucketMinioClientArgs != null && this.BucketMinioClientArgs.ClientArgs != null )
            {
                this.ListenNotificationMinioClientArgs = new ListenBucketNotificationArgs(this.BucketMinioClientArgs.ClientArgs);
                this.ListenNotificationMinioClientArgs.BktClientArgs = new BucketClientArgs();
                this.MinioClientArgs = null;
            }

        }

        public RestRequest GetMakeBucketRequest(string location = "us-east-1")
        {
            if (location == "us-east-1")
            {
                if (this.GetMinioClientArgs().GetRegion() != string.Empty)
                {
                    location = this.GetMinioClientArgs().GetRegion();
                }
            }

            Uri requestUrl = RequestUtil.MakeTargetURL(this.GetMinioClientArgs().GetBaseUrl(), this.GetMinioClientArgs().GetIfSecure(), location);
            this.SetTargetURL(requestUrl);

            var request = new RestRequest("/" + this.BucketMinioClientArgs.BucketName, Method.PUT)
            {
                XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer(),
                RequestFormat = DataFormat.Xml
            };
            // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
            if (location != "us-east-1")
            {
                CreateBucketConfiguration config = new CreateBucketConfiguration(location);
                string body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
                request.AddParameter("text/xml", body, ParameterType.RequestBody);
            }
            return request;
        }

        public RestRequest GetMakeBucketRequest()
        {
            if ( this.BucketMinioClientArgs == null )
            {
                throw new InvalidMinioOperationException("", "Error in Initialization. Bucket parameters not assigned.");
            }
            if (this.BucketMinioClientArgs.Location == "us-east-1")
            {
                if (this.BucketMinioClientArgs.ClientArgs.Region != string.Empty)
                {
                    this.BucketMinioClientArgs.Location = this.GetMinioClientArgs().GetRegion();
                }
            }

            Uri requestUrl = RequestUtil.MakeTargetURL(this.BucketMinioClientArgs.ClientArgs.BaseUrl, this.BucketMinioClientArgs.ClientArgs.Secure, this.BucketMinioClientArgs.Location);
            SetTargetURL(requestUrl);

            var request = new RestRequest("/" + this.BucketMinioClientArgs.BucketName, Method.PUT)
            {
                XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer(),
                RequestFormat = DataFormat.Xml
            };
            CreateBucketConfiguration config = new CreateBucketConfiguration(this.BucketMinioClientArgs.Location);
            string body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
            request.AddParameter("text/xml", body, ParameterType.RequestBody);
            return request;
        }
        public async Task ProcessMakeBucketResponse(RestRequest request, CancellationToken cancellationToken)
        {
            await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<RestRequest> GetVersioningRequest(bool getInfo=false)
        {
            if ( this.BucketMinioClientArgs == null )
            {
                throw new InvalidMinioOperationException("", "Error in Initialization. Bucket parameters not assigned.");
            }
            if ( this.BucketMinioClientArgs.VersioningSuspended && !this.BucketMinioClientArgs.VersioningEnabled )
            {
                throw new InvalidMinioOperationException(this.BucketMinioClientArgs.BucketName, "Versioning has to be enabled to be suspended.");
            }
            if (this.BucketMinioClientArgs.BucketName == null)
            {
                throw new InvalidBucketNameException(this.BucketMinioClientArgs.BucketName, "BucketName cannot be null or empty.");
            }
            RestRequest request;
            if ( getInfo )
            {
                request = await this.CreateRequest(Method.GET, this.BucketMinioClientArgs.BucketName).ConfigureAwait(false);
            }
            else
            {
                request = new RestRequest("/" + this.BucketMinioClientArgs.BucketName, Method.PUT)
                {
                    XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer()
                    {
                        Namespace = "http://s3.amazonaws.com/doc/2006-03-01/",
                        ContentType = "application/xml"
                    },
                    RequestFormat = DataFormat.Xml
                };
                Uri requestUrl = RequestUtil.MakeTargetURL(this.GetMinioClientArgs().GetBaseUrl(), this.GetMinioClientArgs().GetIfSecure(), this.BucketMinioClientArgs.BucketName);
                SetTargetURL(requestUrl);
            }
            request.AddQueryParameter("versioning","");
            if ( !getInfo )
            {
                VersioningConfiguration config = new VersioningConfiguration(true);
                string body = utils.MarshalXML(config, request.XmlSerializer.Namespace);
                request.AddParameter("text/xml", body, ParameterType.RequestBody);
            }
            return request;
        }
        public async Task<VersioningConfiguration> ProcessVersioningResponse(RestRequest request, CancellationToken cancellationToken)
        {
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            VersioningConfiguration config = null;
            if (HttpStatusCode.OK.Equals(response.StatusCode))
            {
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response.Content)))
                {
                    config = (VersioningConfiguration)new XmlSerializer(typeof(VersioningConfiguration)).Deserialize(stream);
                }
                return config;
            }
            return null; 
        }
    }

    public interface IBucketClientArgs: IClientArgs
    {
        string GetBucketName();
        string GetLocation();
        string GetLifecycleConfig();
        Hashtable GetTagKeyValue();
        bool GetVersioningEnabled();
        bool GetVersioningSuspended();

        void SetBucketName(string b);
        void SetLocation(string l);
        void SetLifecycleConfig(string l);
        void SetTagKeyValue(Hashtable t);
        void SetVersioningEnabled(bool e);
        void SetVersioningSuspended(bool s);    
    }
    public class BucketClientArgs : IBucketClientArgs
    {
        internal MinioClientArgs ClientArgs;

        public BucketClientArgs(MinioClientArgs minioClientArgs = null)
        {
            if ( this.ClientArgs == null )
            {
                this.ClientArgs = new MinioClientArgs();
            }
            if ( minioClientArgs != null )
            {
                this.ClientArgs.DeepCopy(minioClientArgs);
            }
        }

        public BucketRegionCache GetRegionCache()
        {
            return ClientArgs.RegionCache;
        }
        public IRequestLogger GetLogger()
        {
            return ClientArgs.Logger;
        }

        public string GetAccessKey()
        {
            return ClientArgs.AccessKey;
        }

        public string GetSecretKey()
        {
            return ClientArgs.SecretKey;
        }

        public string GetBaseUrl()
        {
            return ClientArgs.BaseUrl;
        }
        public string GetEndpoint()
        {
            return ClientArgs.Endpoint;
        }
        public string GetRegion()
        {
            return ClientArgs.Region;
        }
        public Uri GetURI()
        {
            return ClientArgs.uri;
        }
        public bool GetIfSecure()
        {
            return ClientArgs.Secure;
        }

        public void SetBaseUrl(string b)
        {
            ClientArgs.SetBaseUrl(b);
        }
        public void SetEndpoint(string e)
        {
            ClientArgs.SetEndpoint(e);
        }
        public void SetRegion(string r)
        {
            ClientArgs.SetRegion(r);
        }
        public void SetURI(Uri u)
        {
            ClientArgs.SetURI(u);
        }
        public void SetIfSecure(bool s)
        {
            ClientArgs.SetIfSecure(s);
        }
        public void SetRegionCache(BucketRegionCache b)
        {
            ClientArgs.SetRegionCache(b);
        }
        public void SetLogger(IRequestLogger l)
        {
            ClientArgs.SetLogger(l);
        }

        public void SetSecretKey(string s)
        {
            ClientArgs.SecretKey = s;
        }

        public void SetAccessKey(string a)
        {
            ClientArgs.AccessKey = a;
        }

        public string GetBucketName()
        {
            return this.BucketName;
        }
        public string GetLocation()
        {
            return this.Location;
        }
        public string GetLifecycleConfig()
        {
            return this.LifecycleConfig;
        }
        public Hashtable GetTagKeyValue()
        {
            return this.TagKeyValue;
        }
        public bool GetVersioningEnabled()
        {
            return this.VersioningEnabled;
        }
        public bool GetVersioningSuspended()
        {
            return this.VersioningSuspended;
        }

        public void SetBucketName(string b)
        {
            this.BucketName = b;
        }

        public void SetLocation(string l)
        {
            this.Location = l;
        }
        public void SetLifecycleConfig(string l)
        {
            this.LifecycleConfig = l;
        }
        public void SetTagKeyValue(Hashtable t)
        {
            this.TagKeyValue = t;
        }
        public void SetVersioningEnabled(bool e)
        {
            this.VersioningEnabled = e;
        }
        public void SetVersioningSuspended(bool s)
        {
            this.VersioningSuspended = s;
        }
        internal string BucketName { get; set; }
        internal string Location { get; set; }
        internal string LifecycleConfig { get; set; }
        // <String, String>
        internal Hashtable TagKeyValue { get; set; }
        internal bool VersioningEnabled { get; set; }
        internal bool VersioningSuspended { get; set; }

    }

    public class EncryptionClientArgs
    {
        internal MinioClientArgs ClientArgs;
        internal BucketClientArgs BktClientArgs;
        internal SSEC SseConfiguration { get; set; }        
    }

    public class ListenBucketNotificationArgs
    {
        internal MinioClientArgs ClientArgs;
        internal BucketClientArgs BktClientArgs;

        public ListenBucketNotificationArgs(MinioClientArgs minioClientArgs = null)
        {
            if ( this.ClientArgs == null )
            {
                this.ClientArgs = new MinioClientArgs();
            }
            this.ClientArgs.DeepCopy(minioClientArgs);
        }

        public MinioClientArgs MinioClientArgs { get; }
        internal string Prefix { get; set; }
        internal string Suffix { get; set; }
        internal string[] Events { get; set; }

    }
    public class BucketMinioClientBuilder
    {
        public static async Task<MinioClient> GetBucketMinioClient(string bucket, string endPoint, string accessKey, string secretKey)
        {
            var mc =  MinioClient.NewClient(bucket).WithCredentials(accessKey, secretKey).WithEndpoint(endPoint);
            return  await mc.BuildAsync();
        }
        public static async Task<MinioClient> GetBucketMinioClient(string bucket, bool secure, string endPoint, string accessKey, string secretKey)
        {
            var mc = MinioClient.NewClient(bucket).WithCredentials(accessKey, secretKey).WithEndpoint(endPoint).WithSSL();
            return  await mc.BuildAsync();
        }

        public static async Task<MinioClient> GetBucketMinioClient(string bucket, bool secure, string endPoint, int port, string accessKey, string secretKey)
        {
            var mc = MinioClient.NewClient(bucket).WithCredentials(accessKey, secretKey).WithEndpoint(endPoint, port, secure);
            return  await mc.BuildAsync();
        }

        public static async Task<MinioClient> GetListBucketsMinioClient(string endPoint, string accessKey, string secretKey, bool secure)
        {
            MinioClient mc;
            
            if (secure)
            {
                mc =  MinioClient.NewClient().WithCredentials(accessKey, secretKey).WithEndpoint(endPoint).WithSSL();
            }
            else
            {
                mc =  MinioClient.NewClient().WithCredentials(accessKey, secretKey).WithEndpoint(endPoint);
            }
            return await mc.BuildAsync();
        }

        public static async Task<MinioClient> GetListenBucketNotificationMinioClient(string bucket, string prefix, string suffix, string[] events, string endPoint, int port, string accessKey, string secretKey, bool secure)
        {
            MinioClient mc;
            if (port > 0)
            {
                mc =  MinioClient.NewClient(bucket, prefix, suffix, events).WithCredentials(accessKey, secretKey).WithEndpoint(endPoint, port, secure);
            }
            else if ( secure )
            {
                mc =  MinioClient.NewClient(bucket, prefix, suffix, events).WithCredentials(accessKey, secretKey).WithEndpoint(endPoint).WithSSL();
            }
            else
            {
                mc =  MinioClient.NewClient(bucket, prefix, suffix, events).WithCredentials(accessKey, secretKey).WithEndpoint(endPoint);
            }
            return await mc.BuildAsync();
        }
    }
}