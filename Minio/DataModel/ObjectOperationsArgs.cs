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

using RestSharp;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Minio
{
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
        // Each element in the list is a Tuple. Each Tuple has an Object name(string)
        // And the second element is 
        internal List<Tuple<string, List<string>>> ObjectNamesVersions  { get; private set; }

        public RemoveObjectsArgs()
        {
            this.ObjectName = null;
            this.ObjectNames = new List<string>();
            this.ObjectNamesVersions = new List<Tuple<string, List<string>>>();
            this.RequestMethod = Method.POST;
        }

        public RemoveObjectsArgs WithSingleObjectVersions(string name, List<string> versions)
        {
            this.ObjectNamesVersions.Add(new Tuple<string, List<string>>(name, versions));
            return this;
        }
        public RemoveObjectsArgs ClearObjectsWithVersions()
        {
            this.ObjectNamesVersions.Clear();
            return this;
        }

        public RemoveObjectsArgs WithObjectsVersions(List<Tuple<string, List<string>>> namesVersions)
        {
            this.ObjectNamesVersions.AddRange(namesVersions);
            return this;
        }

        public RemoveObjectsArgs WithObjects(List<string> names)
        {
            this.ObjectNames = names;
            return this;
        }

        public override void Validate()
        {
            utils.ValidateBucketName(this.BucketName); // Not doing base.Validate() to skip object name validation.
            if (!string.IsNullOrEmpty(this.ObjectName))
            {
                throw new InvalidOperationException("ObjectName is assigned. Please use WithObjects method to assign multiple objects to be deleted.");
            }
            foreach (Tuple<string, List<string>> item in this.ObjectNamesVersions)
            {
                if (item.Item2.Count == 0)
                {
                    throw new InvalidOperationException("If you are deleting some versions of an object, you have to assign the list of version IDs to remove.");
                }
            }
            if (this.ObjectNames.Count == 0 && this.ObjectNamesVersions.Count == 0)
            {
                throw new InvalidOperationException("Please assign list of object names or object names and version IDs to remove");
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
                    foreach (var version in objTuple.Item2)
                    {
                        objects.Add(new XElement("Object",
                                            new XElement("Key", objTuple.Item1),
                                            new XElement("VersionId", version)));
                    }
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
}