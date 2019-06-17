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

using Minio.DataModel.Tracing;
using System;
using System.Linq;
using System.Text;

namespace Minio
{
    public sealed class DefaultRequestLogger : IRequestLogger
    {
        public void LogRequest(RequestToLog requestToLog, ResponseToLog responseToLog, double durationMs)
        {
            var sb = new StringBuilder("Request completed in ");

            sb.Append(durationMs);
            sb.AppendLine(" ms");

            sb.AppendLine();
            sb.AppendLine("- - - - - - - - - - BEGIN REQUEST - - - - - - - - - -");
            sb.AppendLine();
            sb.Append(requestToLog.method);
            sb.Append(' ');
            sb.Append(requestToLog.uri.ToString());
            sb.AppendLine(" HTTP/1.1");

            var requestHeaders = requestToLog.parameters;
            requestHeaders = requestHeaders.OrderByDescending(p => p.name == "Host");

            foreach (var item in requestHeaders)
            {
                sb.Append(item.name);
                sb.Append(": ");
                sb.AppendLine(item.value.ToString());
            }

            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine("- - - - - - - - - - END REQUEST - - - - - - - - - -");
            sb.AppendLine();

            sb.AppendLine("- - - - - - - - - - BEGIN RESPONSE - - - - - - - - - -");
            sb.AppendLine();

            sb.Append("HTTP/1.1 ");
            sb.Append((int)responseToLog.statusCode);
            sb.Append(' ');
            sb.AppendLine(responseToLog.statusCode.ToString());

            var responseHeaders = responseToLog.headers;

            foreach (var item in responseHeaders)
            {
                sb.Append(item.Name);
                sb.Append(": ");
                sb.AppendLine(item.Value.ToString());
            }

            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine(responseToLog.content);
            sb.AppendLine(responseToLog.errorMessage);

            sb.AppendLine("- - - - - - - - - - END RESPONSE - - - - - - - - - -");

            Console.WriteLine(sb.ToString());
        }
    }
}