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