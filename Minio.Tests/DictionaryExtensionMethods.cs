/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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

using System.Collections.Generic;

namespace Minio.Tests;

/// <summary>
///     Deep compares two dictionaries for equality
/// </summary>
public static class DictionaryExtensionMethods
{
    public static bool PoliciesEqual<String, PolicyType>(this IDictionary<String, PolicyType> first,
        IDictionary<String, PolicyType> second)
    {
        if (first == second) return true;

        if (first == null || second == null) return false;

        if (first.Count != second.Count) return false;

        foreach (var kvp in first)
        {
            var firstValue = kvp.Value;
            if (!second.TryGetValue(kvp.Key, out var secondValue)) return false;

            if (!firstValue.Equals(secondValue)) return false;
        }

        return true;
    }
}