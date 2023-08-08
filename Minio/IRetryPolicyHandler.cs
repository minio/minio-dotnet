using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio;
public interface IRetryPolicyHandler
{
    Task<ResponseResult> Handle(Func<Task<ResponseResult>> executeRequestCallback);
}
