namespace Minio;

public interface IApiResponseErrorHandler
{
    void Handle(ResponseResult response);
}
