/*
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

using Newtera.Helper;

namespace Newtera.DataModel.Args;

public abstract class ObjectArgs<T> : BucketArgs<T>
    where T : ObjectArgs<T>
{
    protected const string S3ZipExtractKey = "X-Newtera-Extract";

    internal string ObjectName { get; set; }
    internal ReadOnlyMemory<byte> RequestBody { get; set; }

    internal string Prefix { get; private set; }

    public T WithObject(string obj)
    {
        ObjectName = obj;
        return (T)this;
    }

    public T WithPrefix(string prefix)
    {
        Prefix = prefix ?? string.Empty;
        return (T) this;
    }

    public T WithRequestBody(ReadOnlyMemory<byte> data)
    {
        RequestBody = data;
        return (T)this;
    }

    internal override void Validate()
    {
        base.Validate();
        Utils.ValidateObjectName(ObjectName);
    }
}
