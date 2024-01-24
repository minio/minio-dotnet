using Minio.DataModel.Result;

namespace Minio.Exceptions;

[Serializable]
public class PreconditionFailedException : MinioException
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
