/*
 * Newtera .NET Library for Newtera TDM, (C) 2017 Newtera, Inc.
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

using System.Xml.Serialization;

namespace Newtera.DataModel.Result;

[Serializable]
[XmlRoot(ElementName = "ListPartsResult", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
public class ListPartsResult
{
    public int NextPartNumberMarker { get; set; }
    public bool IsTruncated { get; set; }
}
