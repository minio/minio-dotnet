using Minio.DataModel.Tracing;

namespace Minio
{
    public interface IRequestLogger
    {
        void LogRequest(RequestToLog requestToLog, ResponseToLog responseToLog, double durationMs);
    }
}