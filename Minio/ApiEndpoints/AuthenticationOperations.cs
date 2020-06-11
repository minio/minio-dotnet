using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

using Minio.DataModel;
using Minio.Exceptions;

using RestSharp;

namespace Minio
{
    public partial class MinioClient : IAuthenticationOperations
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
        public async Task<AssumeRoleResult> AssumeRoleAsync(
            string policy = null,
            TimeSpan? duration = null,
            CancellationToken cancellationToken = default)
        {
            var request = new RestRequest("/", Method.POST);

            // IMPORTANT: order of parameters is critical
            request.AddParameter("Action", "AssumeRole", ParameterType.GetOrPost);
            if (duration != null)
            {
                request.AddParameter("DurationSeconds", (int)duration.Value.TotalSeconds, ParameterType.GetOrPost);
            }
            if (!string.IsNullOrEmpty(policy))
            {
                request.AddParameter("Policy", policy, ParameterType.GetOrPost);
            }
            request.AddParameter("RoleArn", "arn:xxx:xxx:xxx:xxxx", ParameterType.GetOrPost);
            request.AddParameter("RoleSessionName", "anything", ParameterType.GetOrPost);
            request.AddParameter("Version", "2011-06-15", ParameterType.GetOrPost);

            var response = await ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            using (var stream = new MemoryStream(contentBytes))
            {
                var result = (AssumeRoleResponse)new XmlSerializer(typeof(AssumeRoleResponse)).Deserialize(stream);
                return result?.AssumeRoleResult;
            }
        }
    }
}
