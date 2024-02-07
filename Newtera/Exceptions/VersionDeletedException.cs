﻿/*
 * Newtera .NET Library for Newtera TDM, (C) 2020 Newtera, Inc.
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
public class VersionDeletedException : NewteraException
{
    private readonly string versionId;

    public VersionDeletedException(string vid, string message) : base(message)
    {
        versionId = vid;
    }

    public VersionDeletedException(ResponseResult serverResponse) : base(serverResponse)
    {
    }

    public VersionDeletedException(string message) : base(message)
    {
    }

    public VersionDeletedException(string message, ResponseResult serverResponse) : base(message, serverResponse)
    {
    }

    public VersionDeletedException()
    {
    }

    public VersionDeletedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected VersionDeletedException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(
        serializationInfo, streamingContext)
    {
    }

    public override string ToString()
    {
        return $"{versionId}: {base.ToString()}";
    }
}
