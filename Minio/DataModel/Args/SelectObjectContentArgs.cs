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

using System.Text;
using Minio.DataModel.Select;
using Minio.Helper;

namespace Minio.DataModel.Args;

public class SelectObjectContentArgs : EncryptionArgs<SelectObjectContentArgs>
{
    private readonly SelectObjectOptions SelectOptions;

    public SelectObjectContentArgs()
    {
        RequestMethod = HttpMethod.Post;
        SelectOptions = new SelectObjectOptions();
    }

    internal override void Validate()
    {
        base.Validate();
        if (string.IsNullOrEmpty(SelectOptions.Expression))
            throw new InvalidOperationException("The Expression " + nameof(SelectOptions.Expression) +
                                                " for Select Object Content cannot be empty.");

        if (SelectOptions.InputSerialization is null || SelectOptions.OutputSerialization is null)
            throw new InvalidOperationException(
                "The Input/Output serialization members for SelectObjectContentArgs should be initialized " +
                nameof(SelectOptions.InputSerialization) + " " + nameof(SelectOptions.OutputSerialization));
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("select", "");
        requestMessageBuilder.AddQueryParameter("select-type", "2");

        if (RequestBody.IsEmpty)
        {
            RequestBody = Encoding.UTF8.GetBytes(SelectOptions.MarshalXML());
            requestMessageBuilder.SetBody(RequestBody);
        }

        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            Utils.GetMD5SumStr(RequestBody.Span));

        return requestMessageBuilder;
    }

    public SelectObjectContentArgs WithExpressionType(QueryExpressionType e)
    {
        SelectOptions.ExpressionType = e;
        return this;
    }

    public SelectObjectContentArgs WithQueryExpression(string expr)
    {
        SelectOptions.Expression = expr;
        return this;
    }

    public SelectObjectContentArgs WithInputSerialization(SelectObjectInputSerialization serialization)
    {
        SelectOptions.InputSerialization = serialization;
        return this;
    }

    public SelectObjectContentArgs WithOutputSerialization(SelectObjectOutputSerialization serialization)
    {
        SelectOptions.OutputSerialization = serialization;
        return this;
    }

    public SelectObjectContentArgs WithRequestProgress(RequestProgress requestProgress)
    {
        SelectOptions.RequestProgress = requestProgress;
        return this;
    }
}
