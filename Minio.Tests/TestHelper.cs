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

using System;
using System.Text;

namespace Minio.Tests;

internal class TestHelper
{
    private static readonly Random rnd = new();

    // Generate a random string
    public static string GetRandomName(int length = 5)
    {
        var characters = "0123456789abcdefghijklmnopqrstuvwxyz";
        var result = new StringBuilder(length);

        for (var i = 0; i < length; i++) result.Append(characters[rnd.Next(characters.Length)]);
        return result.ToString();
    }
}