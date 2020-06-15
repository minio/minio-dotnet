using System;
using System.Threading;
using System.Threading.Tasks;

using Minio.DataModel;
using Minio.Exceptions;

namespace Minio
{
    public interface IAuthenticationOperations
    {
        /// <summary>
        /// Create an assume role token with given policy and duration.
        /// </summary>
        /// <param name="policy">The token policy.</param>
        /// <param name="duration">The token TTL.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>
        /// Assume role token
        /// </returns>
        /// <exception cref="ForbiddenException">When the current user has no permissions for assume role action.</exception>
        Task<AssumeRoleResult> AssumeRoleAsync(
            string policy = null,
            TimeSpan? duration = null,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
