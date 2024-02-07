/*
 * Newtera .NET Library for Newtera TDM, (C) 2021 Newtera, Inc.
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
public class CredentialsProviderException : NewteraException
{
    private readonly string credentialProviderType;

    public CredentialsProviderException(string credentialProviderType, string message) : base(message)
    {
        this.credentialProviderType = credentialProviderType;
    }

    public CredentialsProviderException(ResponseResult serverResponse) : base(serverResponse)
    {
    }

    public CredentialsProviderException(string message) : base(message)
    {
    }

    public CredentialsProviderException(string message, ResponseResult serverResponse) : base(message, serverResponse)
    {
    }

    public CredentialsProviderException()
    {
    }

    public CredentialsProviderException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected CredentialsProviderException(SerializationInfo serializationInfo, StreamingContext streamingContext) :
        base(serializationInfo, streamingContext)
    {
    }

    public override string ToString()
    {
        return $"{credentialProviderType}: {base.ToString()}";
    }
}
