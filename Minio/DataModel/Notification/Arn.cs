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

namespace Minio.DataModel.Notification;

/// <summary>
///     Arn holds ARN information that will be sent to the web service,
///     ARN desciption can be found in http://docs.aws.amazon.com/general/latest/gr/aws-arns-and-namespaces.html
/// </summary>
public class Arn
{
    [XmlText] private readonly string arnString;

    public Arn()
    {
    }

    /// <summary>
    ///     Pass valid Arn string on aws to constructor
    /// </summary>
    /// <param name="arnString"></param>
    public Arn(string arnString)
    {
        if (string.IsNullOrEmpty(arnString))
            throw new ArgumentException($"'{nameof(arnString)}' cannot be null or empty.", nameof(arnString));

        var parts = arnString.Split(':');
        if (parts.Length == 6)
        {
            Partition = parts[1];
            Service = parts[2];
            Region = parts[3];
            AccountID = parts[4];
            Resource = parts[5];
            this.arnString = arnString;
        }
    }

    /// <summary>
    ///     Constructs new ARN based on the given partition, service, region, account id and resource
    /// </summary>
    /// <param name="partition"></param>
    /// <param name="service"></param>
    /// <param name="region"></param>
    /// <param name="accountId"></param>
    /// <param name="resource"></param>
    public Arn(string partition, string service, string region, string accountId, string resource)
    {
        Partition = partition;
        Service = service;
        Region = region;
        AccountID = accountId;
        Resource = resource;
        arnString = $"arn:{Partition}:{Service}:{Region}:{AccountID}:{Resource}";
    }

    private string Partition { get; }
    private string Service { get; }
    private string Region { get; }
    private string AccountID { get; }
    private string Resource { get; }

    public override string ToString()
    {
        return arnString;
    }
}