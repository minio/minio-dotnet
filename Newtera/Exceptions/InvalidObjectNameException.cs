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

using System.Runtime.Serialization;
using Newtera.DataModel.Result;

namespace Newtera.Exceptions;

[Serializable]
public class InvalidObjectNameException : NewteraException
{
    private readonly string objectName;

    public InvalidObjectNameException(string objectName, string message) : base(message)
    {
        this.objectName = objectName;
    }

    public InvalidObjectNameException(ResponseResult serverResponse) : base(serverResponse)
    {
    }

    public InvalidObjectNameException(string message) : base(message)
    {
    }

    public InvalidObjectNameException(string message, ResponseResult serverResponse) : base(message, serverResponse)
    {
    }

    public InvalidObjectNameException()
    {
    }

    public InvalidObjectNameException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected InvalidObjectNameException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(
        serializationInfo, streamingContext)
    {
    }

    public override string ToString()
    {
        return $"{objectName}: {base.ToString()}";
    }
}
