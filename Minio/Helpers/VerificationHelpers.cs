namespace Minio.Helpers;

/// <summary>
/// Provides helper methods for validating MinIO and S3-compatible resource names.
/// </summary>
public static class VerificationHelpers
{
    /// <summary>
    /// Validates that the given bucket name conforms to the S3 bucket naming rules
    /// defined at <see href="https://docs.aws.amazon.com/AmazonS3/latest/userguide/bucketnamingrules.html"/>.
    /// </summary>
    /// <remarks>
    /// The following rules are enforced:
    /// <list type="bullet">
    ///   <item><description>Names must be between 3 and 63 characters long.</description></item>
    ///   <item><description>Names may only contain lowercase letters, digits, dots (<c>.</c>), and hyphens (<c>-</c>).</description></item>
    ///   <item><description>Names must begin and end with a letter or digit.</description></item>
    ///   <item><description>Names must not contain two adjacent dots.</description></item>
    ///   <item><description>Names must not start with the prefix <c>xn--</c>.</description></item>
    ///   <item><description>Names must not start with the prefix <c>sthree-</c>.</description></item>
    ///   <item><description>Names must not end with the suffix <c>-s3alias</c>.</description></item>
    ///   <item><description>Names must not end with the suffix <c>--ol-s3</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="bucketName">The bucket name to validate.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="bucketName"/> satisfies all S3 naming rules;
    /// otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bucketName"/> is <see langword="null"/>.</exception>
    public static bool VerifyBucketName(string bucketName)
    {
        if (bucketName == null) 
            throw new ArgumentNullException(nameof(bucketName));
        
        // Bucket names must be between 3 (min) and 63 (max) characters long
        if (bucketName.Length < 3 || bucketName.Length > 63) 
            return false;
        
        for (var i=0; i<bucketName.Length; ++i)
        {
            var ch = bucketName[i];
            
            // Bucket names can consist only of lowercase letters, numbers, dots (.), and hyphens (-)
            if (!char.IsLower(ch) && !char.IsDigit(ch) && ch != '.' && ch != '-')
                return false;
            
            // Bucket names must begin and end with a letter or number
            if ((i == 0 || i == bucketName.Length - 1) && !char.IsLower(ch) && !char.IsDigit(ch))
                return false;
            
            // Bucket names must not contain two adjacent periods 
            if (i > 0 && ch == '.' && bucketName[i - 1] == '.')
                return false;
        }
        
        // Bucket names must not start with the prefix xn--
        if (bucketName.StartsWith("xn--", StringComparison.Ordinal)) return false;

        // Bucket names must not start with the prefix sthree- and the prefix sthree-configurator
        if (bucketName.StartsWith("sthree-", StringComparison.Ordinal)) return false;

        // Bucket names must not end with the suffix -s3alias
        if (bucketName.EndsWith("-s3alias", StringComparison.Ordinal)) return false;

        // Bucket names must not end with the suffix --ol-s3
        if (bucketName.EndsWith("--ol-s3", StringComparison.Ordinal)) return false;

        return true;
    }
}