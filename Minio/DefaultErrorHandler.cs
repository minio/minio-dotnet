using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Minio;
public class DefaultErrorHandler : IApiResponseErrorHandler
{
    public void Handle(ResponseResult response)
    {
        if (response.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.BadRequest)
            MinioClient.ParseError(response);
    }
}
