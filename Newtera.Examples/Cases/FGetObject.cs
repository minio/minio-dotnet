/*
 * Newtera .NET Library for Newtera TDM, (C) 2017-2021 Newtera, Inc.
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

using Newtera.DataModel.Args;

namespace Newtera.Examples.Cases;

internal static class FGetObject
{
    // Download object from bucket into local file
    public static async Task Run(INewteraClient newtera,
        string bucketName = "my-bucket-name",
        string objectName = "my-object-name",
        string fileName = "local-filename")
    {
        try
        {
            Console.WriteLine("Running example for API: GetObjectAsync");
            File.Delete(fileName);
            var args = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithFile(fileName);
            _ = await newtera.GetObjectAsync(args).ConfigureAwait(false);
            Console.WriteLine($"Downloaded the file {fileName} from bucket {bucketName}");
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}
