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

public class StatObjectArgs : ObjectConditionalQueryArgs<StatObjectArgs>
{
    public StatObjectArgs()
    {
        RequestMethod = HttpMethod.Head;
    }

    internal long ObjectOffset { get; private set; }
    internal long ObjectLength { get; private set; }
    internal bool OffsetLengthSet { get; set; }

    internal override void Validate()
    {
        base.Validate();
        if (!string.IsNullOrEmpty(NotMatchETag) && !string.IsNullOrEmpty(MatchETag))
            throw new InvalidOperationException("Invalid to set both Etag match conditions " + nameof(NotMatchETag) +
                                                " and " + nameof(MatchETag));

        if (!ModifiedSince.Equals(default) &&
            !UnModifiedSince.Equals(default))
            throw new InvalidOperationException("Invalid to set both modified date match conditions " +
                                                nameof(ModifiedSince) + " and " + nameof(UnModifiedSince));

        if (OffsetLengthSet)
        {
            if (ObjectOffset < 0 || ObjectLength < 0)
                throw new InvalidDataException(nameof(ObjectOffset) + " and " + nameof(ObjectLength) +
                                               "cannot be less than 0.");

            if (ObjectOffset == 0 && ObjectLength == 0)
                throw new InvalidDataException("Either " + nameof(ObjectOffset) + " or " + nameof(ObjectLength) +
                                               " must be greater than 0.");
        }

        Populate();
    }

    private void Populate()
    {
        Headers ??= new Dictionary<string, string>(StringComparer.Ordinal);
        if (OffsetLengthSet)
        {
            // "Range" header accepts byte start index and end index
            if (ObjectLength > 0 && ObjectOffset > 0)
                Headers["Range"] = "bytes=" + ObjectOffset + "-" + (ObjectOffset + ObjectLength - 1);
            else if (ObjectLength == 0 && ObjectOffset > 0)
                Headers["Range"] = "bytes=" + ObjectOffset + "-";
            else if (ObjectLength > 0 && ObjectOffset == 0) Headers["Range"] = "bytes=0-" + (ObjectLength - 1);
        }
    }

    public StatObjectArgs WithOffsetAndLength(long offset, long length)
    {
        OffsetLengthSet = true;
        ObjectOffset = offset < 0 ? 0 : offset;
        ObjectLength = length < 0 ? 0 : length;
        return this;
    }

    public StatObjectArgs WithLength(long length)
    {
        OffsetLengthSet = true;
        ObjectOffset = 0;
        ObjectLength = length < 0 ? 0 : length;
        return this;
    }
}
