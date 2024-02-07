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
        // Avoiding null values. Default is empty strings.
        Delimiter = string.Empty;
        Prefix = string.Empty;
        UseV2 = true;
        Versions = false;
        Marker = string.Empty;
    }

    internal string Delimiter { get; private set; }
    internal string Prefix { get; private set; }
    internal bool UseV2 { get; private set; }
    internal string Marker { get; private set; }
    internal string VersionIdMarker { get; private set; }
    internal bool Versions { get; private set; }
    internal string ContinuationToken { get; set; }

    public GetObjectListArgs WithDelimiter(string delim)
    {
        Delimiter = delim ?? string.Empty;
        return this;
    }

    public GetObjectListArgs WithPrefix(string prefix)
    {
        Prefix = prefix ?? string.Empty;
        return this;
    }

    public GetObjectListArgs WithMarker(string marker)
    {
        Marker = string.IsNullOrWhiteSpace(marker) ? string.Empty : marker;
        return this;
    }

    public GetObjectListArgs WithVersionIdMarker(string marker)
    {
        VersionIdMarker = string.IsNullOrWhiteSpace(marker) ? string.Empty : marker;
        return this;
    }

    public GetObjectListArgs WithVersions(bool versions)
    {
        Versions = versions;
        return this;
    }

    public GetObjectListArgs WithContinuationToken(string token)
    {
        ContinuationToken = string.IsNullOrWhiteSpace(token) ? string.Empty : token;
        return this;
    }

    public GetObjectListArgs WithListObjectsV1(bool useV1)
    {
        UseV2 = !useV1;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        foreach (var h in Headers)
            requestMessageBuilder.AddOrUpdateHeaderParameter(h.Key, h.Value);

        requestMessageBuilder.AddQueryParameter("delimiter", Delimiter);
        requestMessageBuilder.AddQueryParameter("max-keys", "1000");
        requestMessageBuilder.AddQueryParameter("encoding-type", "url");
        requestMessageBuilder.AddQueryParameter("prefix", Prefix);
        if (Versions)
        {
            requestMessageBuilder.AddQueryParameter("versions", "");
            if (!string.IsNullOrWhiteSpace(Marker)) requestMessageBuilder.AddQueryParameter("key-marker", Marker);
            if (!string.IsNullOrWhiteSpace(VersionIdMarker))
                requestMessageBuilder.AddQueryParameter("version-id-marker", VersionIdMarker);
        }
        else if (!Versions && UseV2)
        {
            requestMessageBuilder.AddQueryParameter("list-type", "2");
            if (!string.IsNullOrWhiteSpace(Marker)) requestMessageBuilder.AddQueryParameter("start-after", Marker);
            if (!string.IsNullOrWhiteSpace(ContinuationToken))
                requestMessageBuilder.AddQueryParameter("continuation-token", ContinuationToken);
        }
        else if (!Versions && !UseV2)
        {
            requestMessageBuilder.AddQueryParameter("marker", Marker);
        }
        else
        {
            throw new InvalidOperationException("Wrong set of properties set.");
        }

        return requestMessageBuilder;
    }
}
