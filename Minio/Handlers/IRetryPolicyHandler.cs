using Minio.DataModel.Result;

namespace Minio.Handlers;

public interface IRetryPolicyHandler
{
    Task<ResponseResult> Handle(Func<Task<ResponseResult>> executeRequestCallback);
}
