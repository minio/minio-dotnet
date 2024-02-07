using Newtera.DataModel.Result;

namespace Newtera.Handlers;

public interface IRetryPolicyHandler
{
    Task<ResponseResult> Handle(Func<Task<ResponseResult>> executeRequestCallback);
}
