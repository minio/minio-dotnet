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

using System.IO;
using System.Xml.Serialization;

namespace Minio;

/// <summary>
/// Represents a response where the content is XML.
/// </summary>
/// <typeparam name="TResult">The type of the XML content</typeparam>
public abstract class GenericXmlResponse<TResult> : GenericResponse
{
    /// <summary>
    /// The parsed result, or null if the 
    /// </summary>
    protected TResult _result;

    public GenericXmlResponse(ResponseResult result) : base(result)
    {
        if (!IsOkWithContent)
        {
            return;
        }

        using var stream = new MemoryStream(result.ContentBytes);
        var serializer = new XmlSerializer(typeof(TResult));
        _result = (TResult)serializer.Deserialize(stream);
    }

    /// <summary>
    /// Convert the content if required.
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    protected virtual string ConvertContent(string content) => content;
}
