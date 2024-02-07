/*
 * Newtera .NET Library for Newtera TDM,
 * (C) 2017, 2018, 2019, 2020 Newtera, Inc.
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

using System.Runtime.Serialization;

namespace Newtera.Exceptions;

[Serializable]
public class MalFormedXMLException : Exception
{
    internal string bucketName;
    internal string key;
    internal string resource;

    public MalFormedXMLException()
    {
    }

    public MalFormedXMLException(string message) : base(message)
    {
    }

    public MalFormedXMLException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public MalFormedXMLException(string resource, string bucketName, string message, string keyName = null) :
        base(message)
    {
        this.resource = resource;
        this.bucketName = bucketName;
        key = keyName;
    }

    protected MalFormedXMLException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(
        serializationInfo, streamingContext)
    {
    }
}
