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

using System.Text;
using Minio.DataModel.Tracing;

namespace Minio.Handlers;

public sealed class DefaultRequestLogger : IRequestLogger
{
    public void LogRequest(RequestToLog requestToLog, ResponseToLog responseToLog, double durationMs)
    {
        if (requestToLog is null) throw new ArgumentNullException(nameof(requestToLog));

        if (responseToLog is null) throw new ArgumentNullException(nameof(responseToLog));

        var sb = new StringBuilder("Request completed in ");

        _ = sb.Append(durationMs);
        _ = sb.AppendLine(" ms");

        _ = sb.AppendLine();
        _ = sb.AppendLine("- - - - - - - - - - BEGIN REQUEST - - - - - - - - - -");
        _ = sb.AppendLine();
        _ = sb.Append(requestToLog.Method);
        _ = sb.Append(' ');
        _ = sb.Append(requestToLog.Uri);
        _ = sb.AppendLine(" HTTP/1.1");

        var requestHeaders = requestToLog.Parameters;
        requestHeaders =
            requestHeaders.OrderByDescending(p => string.Equals(p.Name, "Host", StringComparison.OrdinalIgnoreCase));

        foreach (var item in requestHeaders)
        {
            _ = sb.Append(item.Name);
            _ = sb.Append(": ");
            _ = sb.AppendLine(item.Value.ToString());
        }

        _ = sb.AppendLine();
        _ = sb.AppendLine();

        _ = sb.AppendLine("- - - - - - - - - - END REQUEST - - - - - - - - - -");
        _ = sb.AppendLine();

        _ = sb.AppendLine("- - - - - - - - - - BEGIN RESPONSE - - - - - - - - - -");
        _ = sb.AppendLine();

        _ = sb.Append("HTTP/1.1 ");
        _ = sb.Append((int)responseToLog.StatusCode);
        _ = sb.Append(' ');
        _ = sb.AppendLine(responseToLog.StatusCode.ToString());

        var responseHeaders = responseToLog.Headers;

        foreach (var item in responseHeaders)
        {
            _ = sb.Append(item.Key);
            _ = sb.Append(": ");
            _ = sb.AppendLine(item.Value);
        }

        _ = sb.AppendLine();
        _ = sb.AppendLine();

        _ = sb.AppendLine(responseToLog.Content);
        _ = sb.AppendLine(responseToLog.ErrorMessage);

        _ = sb.AppendLine("- - - - - - - - - - END RESPONSE - - - - - - - - - -");

        Console.WriteLine(sb.ToString());
    }
}
