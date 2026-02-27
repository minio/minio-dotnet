namespace Minio.CredentialProviders;

/// <summary>
/// An <see cref="ICredentialsProvider"/> implementation that reads credentials from environment variables.
/// The access key is resolved from <c>MINIO_ROOT_USER</c>, <c>MINIO_ACCESS_KEY</c>, or
/// <c>AWS_ACCESS_KEY_ID</c> (in that order of precedence). The secret key is resolved from
/// <c>MINIO_ROOT_PASSWORD</c>, <c>MINIO_SECRET_KEY</c>, or <c>AWS_SECRET_ACCESS_KEY</c>.
/// An optional session token is read from <c>AWS_SESSION_TOKEN</c>.
/// </summary>
public class EnvironmentCredentialsProvider : ICredentialsProvider
{
    /// <summary>
    /// Reads credentials from well-known environment variables and returns them as a <see cref="Credentials"/> instance.
    /// Throws an <see cref="InvalidOperationException"/> if neither the access key nor the secret key
    /// can be resolved from the environment.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests (not used by this implementation).</param>
    /// <returns>
    /// A <see cref="ValueTask{T}"/> that resolves synchronously to a <see cref="Credentials"/> instance
    /// populated from the current process environment.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the access key or secret key cannot be found in any of the supported environment variables.
    /// </exception>
    public ValueTask<Credentials> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        var accessKey = GetEnvironmentString("MINIO_ROOT_USER", "MINIO_ACCESS_KEY", "AWS_ACCESS_KEY_ID");
        var secretKey = GetEnvironmentString("MINIO_ROOT_PASSWORD", "MINIO_SECRET_KEY", "AWS_SECRET_ACCESS_KEY");
        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey)) throw new InvalidOperationException("No access key or secret key");
        var sessionToken = GetEnvironmentString("AWS_SESSION_TOKEN") ?? string.Empty;
        return new ValueTask<Credentials>(new Credentials(accessKey, secretKey, sessionToken));
    }

    private static string? GetEnvironmentString(params string[] variables)
    {
        foreach (var variable in variables)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrEmpty(value)) return value;
        }
        return null;
    }
}
