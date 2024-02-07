using Newtera.DataModel.Result;

namespace Newtera.Handlers;

public interface IApiResponseErrorHandler
{
    void Handle(ResponseResult response);
}
