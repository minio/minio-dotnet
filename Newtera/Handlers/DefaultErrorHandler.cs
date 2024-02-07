using System.Net;
using Newtera.DataModel.Result;

namespace Newtera.Handlers;

public class DefaultErrorHandler : IApiResponseErrorHandler
{
    public void Handle(ResponseResult response)
    {
        if (response is null) throw new ArgumentNullException(nameof(response));

        if (response.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.BadRequest)
            NewteraClient.ParseError(response);
    }
}
