using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Xml.Serialization;
using Minio.DataModel;
using RestSharp;


namespace Minio
{
    public class ObjectArgs : BucketArgs
    {
        internal string ObjectName { get; set; }

        public ObjectArgs(string bucket, string obj)
                    : base(bucket)
        {
            this.ObjectName = obj;
        }

    }

    public class GetObjectLegalHoldArgs : ObjectArgs
    {
        internal string VersionId { get; set; }

        public GetObjectLegalHoldArgs(string bucket, string obj)
                        : base(bucket, obj)
        {
        }
        public new void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
            utils.ValidateObjectName(this.ObjectName);
        }
        public GetObjectLegalHoldArgs WithVersionID(string v)
        {
            this.VersionId = v;
            return this;
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
    public class SetObjectLegalHoldArgs : ObjectArgs
    {
        internal bool LegalHoldON { get; set; }
        internal string VersionId { get; set; }

        public SetObjectLegalHoldArgs(string bucket, string obj)
                        : base(bucket, obj)
        {
            this.LegalHoldON = false;
        }
        public new void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
            utils.ValidateObjectName(this.ObjectName);
        }
        public SetObjectLegalHoldArgs WithVersionID(string v)
        {
            this.VersionId = v;
            return this;
        }

        public SetObjectLegalHoldArgs WithLegalHold(bool status)
        {
            this.LegalHoldON = status;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            ObjectLegalHoldConfiguration config = new ObjectLegalHoldConfiguration(this.LegalHoldON);
            string body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
            request.AddParameter(new Parameter("text/xml", body, ParameterType.RequestBody));
            request.AddQueryParameter("legal-hold", "");
            if( !string.IsNullOrEmpty(this.VersionId) )
            {
                request.AddQueryParameter("versionId", this.VersionId);
            }
            var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(body));
            string base64 = Convert.ToBase64String(hash);
            request.AddOrUpdateParameter("Content-MD5", base64, ParameterType.HttpHeader);
            return request;
        }

        public ObjectLegalHoldConfiguration ProcessResponse(RestSharp.IRestResponse response)
        {
            if (!HttpStatusCode.OK.Equals(response.StatusCode))
            {
                return null;
            }
            ObjectLegalHoldConfiguration lhRes = null;
            try
            {
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response.Content)))
                {
                    lhRes = (ObjectLegalHoldConfiguration)new XmlSerializer(typeof(ObjectLegalHoldConfiguration)).Deserialize(stream);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return lhRes;
        }
    }
}