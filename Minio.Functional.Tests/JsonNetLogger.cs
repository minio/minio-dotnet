using System;
using Minio.DataModel.Tracing;
using Newtonsoft.Json;

namespace Minio.Functional.Tests
{
    internal class JsonNetLogger: IRequestLogger
    {
        public void LogRequest(RequestToLog requestToLog, ResponseToLog responseToLog, double durationMs)
        {
            Console.Out.WriteLine(string.Format("Request completed in {0} ms, Request: {1}, Response: {2}",
                durationMs,
                JsonConvert.SerializeObject(requestToLog, Formatting.Indented),
                JsonConvert.SerializeObject(responseToLog, Formatting.Indented)));
        }
    }
}
