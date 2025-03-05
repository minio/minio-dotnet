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

namespace Minio.DataModel.Args;

public class ListObjectsArgs : BucketArgs<ListObjectsArgs>
{
    public ListObjectsArgs()
    {
        UseV2 = true;
        Versions = false;
        IncludeUserMetadata = false;
    }

    internal string Prefix { get; private set; }
    internal bool Recursive { get; private set; }
    internal bool Versions { get; private set; }
    internal bool IncludeUserMetadata { get; private set; }
    internal bool UseV2 { get; private set; }
    internal string Namespace { get; private set; }

    public ListObjectsArgs WithPrefix(string prefix)
    {
        Prefix = prefix;
        return this;
    }

    public ListObjectsArgs WithRecursive(bool rec)
    {
        Recursive = rec;
        return this;
    }

    public ListObjectsArgs WithVersions(bool ver)
    {
        Versions = ver;
        return this;
    }

    public ListObjectsArgs WithIncludeUserMetadata(bool met)
    {
        IncludeUserMetadata = met;
        return this;
    }

    public ListObjectsArgs WithListObjectsV1(bool useV1)
    {
        UseV2 = !useV1;
        return this;
    }
  
    public ListObjectsArgs WithNamespace(string ns)
    {
        Namespace = ns;
        return this;
    }
}
