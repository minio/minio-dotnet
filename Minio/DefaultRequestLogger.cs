/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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
using Minio.DataModel.Tracing;

namespace Minio
{
    internal sealed class DefaultRequestLogger : IRequestLogger
    {
        public void LogRequest(RequestToLog requestToLog, ResponseToLog responseToLog, double durationMs)
        {
            var sb = new StringBuilder("Request completed in ");

            sb.Append(durationMs);
            sb.AppendLine(" ms, ");
            sb.AppendLine("Request: ");

            sb.Append(" method: "); sb.AppendLine(requestToLog.method);
            sb.Append(" uri: "); sb.AppendLine(requestToLog.uri.ToString());
            sb.Append(" resource: "); sb.AppendLine(requestToLog.resource);

            sb.Append(" parameters: ");

            foreach (var item in requestToLog.parameters)
            {
                sb.Append("  name:"); sb.AppendLine(item.name);
                sb.Append("  type:"); sb.AppendLine(item.type);
                sb.Append("  value:"); sb.AppendLine(item.value.ToString());
            }

            sb.AppendLine("Response: ");
            sb.Append(" statusCode: "); sb.AppendLine(responseToLog.statusCode.ToString());
            sb.Append(" responseUri: "); sb.AppendLine(responseToLog.responseUri.ToString());
            sb.Append(" headers: ");

            foreach (RestSharp.Parameter item in responseToLog.headers)
            {
                sb.Append("  name:"); sb.AppendLine(item.Name);
                sb.Append("  value:"); sb.AppendLine(item.Value.ToString());
            }

            sb.Append(" content: "); sb.AppendLine(responseToLog.content);
            sb.Append(" errorMessage: "); sb.AppendLine(responseToLog.errorMessage);

            Console.Out.WriteLine(sb.ToString());
        }
    }
}