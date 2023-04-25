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

using System.Xml.Serialization;

namespace Minio.DataModel;

/// <summary>
///     LambdaConfig carries one single cloudfunction notification configuration
/// </summary>
[Serializable]
public class LambdaConfig : NotificationConfiguration
{
    public LambdaConfig()
    {
    }

    public LambdaConfig(string arn) : base(arn)
    {
        Lambda = arn;
    }

    public LambdaConfig(Arn arn) : base(arn)
    {
        Lambda = arn.ToString();
    }

    [XmlElement("CloudFunction")] public string Lambda { get; set; }

    // Implement equality for this object
    public override bool Equals(object obj)
    {
        var other = (LambdaConfig)obj;
        // If parameter is null return false.
        if (obj is null)
            return false;
        return other.Lambda.Equals(Lambda, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return Lambda.GetHashCode();
    }
}