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
public class InvalidEndpointException : NewteraException
{
    private readonly string endpoint;

    public InvalidEndpointException(string endpoint, string message) : base(message)
    {
        this.endpoint = endpoint;
    }

    public InvalidEndpointException(string message) : base(message)
    {
    }

    public InvalidEndpointException(ResponseResult serverResponse) : base(serverResponse)
    {
    }

    public InvalidEndpointException(string message, ResponseResult serverResponse) : base(message, serverResponse)
    {
    }

    public InvalidEndpointException()
    {
    }

    public InvalidEndpointException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected InvalidEndpointException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(
        serializationInfo, streamingContext)
    {
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(endpoint))
            return base.ToString();
        return $"{endpoint}: {base.ToString()}";
    }
}
