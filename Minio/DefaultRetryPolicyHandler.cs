namespace Minio;

public class DefaultRetryPolicyHandler : IRetryPolicyHandler
{
    public DefaultRetryPolicyHandler()
    {
    }

    public DefaultRetryPolicyHandler(Func<Func<Task<ResponseResult>>, Task<ResponseResult>> retryPolicyHandler)
    {
        RetryPolicyHandler = retryPolicyHandler;
    }

    public Func<Func<Task<ResponseResult>>, Task<ResponseResult>> RetryPolicyHandler { get; }

    public virtual Task<ResponseResult> Handle(Func<Task<ResponseResult>> executeRequestCallback)
    {
        if (RetryPolicyHandler is not null)
            return RetryPolicyHandler.Invoke(executeRequestCallback);

        return executeRequestCallback.Invoke();
    }
}
