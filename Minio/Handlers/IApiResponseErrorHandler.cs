using Minio.DataModel.Result;

namespace Minio.Handlers;

#pragma warning disable 0067
public interface IApiResponseErrorHandler
#pragma warning disable 0067

{
    void Handle(ResponseResult responseResult);
}
