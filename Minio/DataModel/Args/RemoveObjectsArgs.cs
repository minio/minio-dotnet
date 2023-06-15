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

using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Minio.DataModel.Args;

public class RemoveObjectsArgs : ObjectArgs<RemoveObjectsArgs>
{
    public RemoveObjectsArgs()
    {
        ObjectName = null;
        ObjectNames = new List<string>();
        ObjectNamesVersions = new List<Tuple<string, string>>();
        RequestMethod = HttpMethod.Post;
    }

    internal IList<string> ObjectNames { get; private set; }

    // Each element in the list is a Tuple. Each Tuple has an Object name & the version ID.
    internal List<Tuple<string, string>> ObjectNamesVersions { get; }

    public RemoveObjectsArgs WithObjectAndVersions(string objectName, IList<string> versions)
    {
        if (versions is null)
            throw new ArgumentNullException(nameof(versions));

        foreach (var vid in versions)
            ObjectNamesVersions.Add(new Tuple<string, string>(objectName, vid));
        return this;
    }

    // Tuple<string, List<string>>. Tuple object name -> List of Version IDs.
    public RemoveObjectsArgs WithObjectsVersions(IList<Tuple<string, List<string>>> objectsVersionsList)
    {
        if (objectsVersionsList is null)
            throw new ArgumentNullException(nameof(objectsVersionsList));

        foreach (var objVersions in objectsVersionsList)
            foreach (var vid in objVersions.Item2)
                ObjectNamesVersions.Add(new Tuple<string, string>(objVersions.Item1, vid));

        return this;
    }

    public RemoveObjectsArgs WithObjectsVersions(IList<Tuple<string, string>> objectVersions)
    {
        ObjectNamesVersions.AddRange(objectVersions);
        return this;
    }

    public RemoveObjectsArgs WithObjects(IList<string> names)
    {
        ObjectNames = names;
        return this;
    }

    internal override void Validate()
    {
        // Skip object name validation.
        Utils.ValidateBucketName(BucketName);
        if (!string.IsNullOrEmpty(ObjectName))
            throw new InvalidOperationException(nameof(ObjectName) + " is set. Please use " + nameof(WithObjects) +
                                                "or " +
                                                nameof(WithObjectsVersions) + " method to set objects to be deleted.");

        if (ObjectNames is null && ObjectNamesVersions is null ||
            ObjectNames.Count == 0 && ObjectNamesVersions.Count == 0)
            throw new InvalidOperationException(
                "Please assign list of object names or object names and version IDs to remove using method(s) " +
                nameof(WithObjects) + " " + nameof(WithObjectsVersions));
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        var objects = new List<XElement>();
        requestMessageBuilder.AddQueryParameter("delete", "");
        XElement deleteObjectsRequest;
        if (ObjectNamesVersions.Count > 0)
        {
            // Object(s) & multiple versions
            foreach (var objTuple in ObjectNamesVersions)
                objects.Add(new XElement("Object",
                    new XElement("Key", objTuple.Item1),
                    new XElement("VersionId", objTuple.Item2)));

            deleteObjectsRequest = new XElement("Delete", objects,
                new XElement("Quiet", true));
            requestMessageBuilder.AddXmlBody(Convert.ToString(deleteObjectsRequest, CultureInfo.InvariantCulture));
        }
        else
        {
            // Multiple Objects
            foreach (var obj in ObjectNames)
                objects.Add(new XElement("Object",
                    new XElement("Key", obj)));

            deleteObjectsRequest = new XElement("Delete", objects,
                new XElement("Quiet", true));
            requestMessageBuilder.AddXmlBody(Convert.ToString(deleteObjectsRequest, CultureInfo.InvariantCulture));
        }

        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            Utils.GetMD5SumStr(
                Encoding.UTF8.GetBytes(Convert.ToString(deleteObjectsRequest, CultureInfo.InvariantCulture))));

        return requestMessageBuilder;
    }
}
