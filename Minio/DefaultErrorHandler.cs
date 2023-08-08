using System.Net;

namespace Minio;

public class DefaultErrorHandler : IApiResponseErrorHandler
{
    public void Handle(ResponseResult response)
    {
        if (response.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.BadRequest)
            MinioClient.ParseError(response);
    }
}
