namespace Minio;

public interface IRetryPolicyHandler
{
    Task<ResponseResult> Handle(Func<Task<ResponseResult>> executeRequestCallback);
}
