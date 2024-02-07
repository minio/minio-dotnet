using Newtera.DataModel.Result;

namespace Newtera.Exceptions;

[Serializable]
public class PreconditionFailedException : NewteraException
{
    public PreconditionFailedException()
    {
    }

    public PreconditionFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public PreconditionFailedException(ResponseResult serverResponse) : base(serverResponse)
    {
    }

    public PreconditionFailedException(string message) : base(message)
    {
    }

    public PreconditionFailedException(string message, ResponseResult serverResponse) : base(message, serverResponse)
    {
    }
}
