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

public class ListIncompleteUploadsArgs : BucketArgs<ListIncompleteUploadsArgs>
{
    public ListIncompleteUploadsArgs()
    {
        RequestMethod = HttpMethod.Get;
        Recursive = true;
    }

    internal string Prefix { get; private set; }
    internal string Delimiter { get; private set; }
    internal bool Recursive { get; private set; }

    public ListIncompleteUploadsArgs WithPrefix(string prefix)
    {
        Prefix = prefix ?? string.Empty;
        return this;
    }

    public ListIncompleteUploadsArgs WithDelimiter(string delim)
    {
        Delimiter = delim ?? string.Empty;
        return this;
    }

    public ListIncompleteUploadsArgs WithRecursive(bool recursive)
    {
        Recursive = recursive;
        Delimiter = recursive ? string.Empty : "/";
        return this;
    }
}
