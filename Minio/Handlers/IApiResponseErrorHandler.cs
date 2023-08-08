using Minio.DataModel.Result;

namespace Minio.Handlers;

public interface IApiResponseErrorHandler
{
    void Handle(ResponseResult response);
}
