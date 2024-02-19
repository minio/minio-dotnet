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

public class ListObjectsArgs : BucketArgs<ListObjectsArgs>
{
    public ListObjectsArgs()
    {
        RequestPath = "/api/blob/objects/";
    }

    internal string Prefix { get; private set; }
    internal bool Recursive { get; private set; }

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
}
