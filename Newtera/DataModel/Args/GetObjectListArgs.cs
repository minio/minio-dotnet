/*
 * Newtera .NET Library for Newtera TDM, (C) 2020, 2021 Newtera, Inc.
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

namespace Newtera.DataModel.Args;

internal class GetObjectListArgs : BucketArgs<GetObjectListArgs>
{
    public GetObjectListArgs()
    {
        RequestMethod = HttpMethod.Get;
        RequestPath = "/api/blob/objects/";
        Prefix = string.Empty;
    }

    internal string Prefix { get; private set; }
    internal string ContinuationToken { get; set; }

    public GetObjectListArgs WithPrefix(string prefix)
    {
        Prefix = prefix ?? string.Empty;
        return this;
    }

    public GetObjectListArgs WithContinuationToken(string token)
    {
        ContinuationToken = string.IsNullOrWhiteSpace(token) ? string.Empty : token;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        foreach (var h in Headers)
            requestMessageBuilder.AddOrUpdateHeaderParameter(h.Key, h.Value);

        requestMessageBuilder.AddQueryParameter("max-keys", "1000");
        requestMessageBuilder.AddQueryParameter("prefix", Prefix);

        return requestMessageBuilder;
    }
}
