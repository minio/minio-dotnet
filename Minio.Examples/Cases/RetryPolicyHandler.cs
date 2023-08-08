using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polly;

namespace Minio.Examples.Cases;
public class RetryPolicyHandler : IRetryPolicyHandler
{
    private readonly AsyncPolicy<ResponseResult> policy;

    public RetryPolicyHandler(AsyncPolicy<ResponseResult> policy)
    {
        this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public Task<ResponseResult> Handle(Func<Task<ResponseResult>> executeRequestCallback)
    {
        return policy.ExecuteAsync(executeRequestCallback);
    }
}
