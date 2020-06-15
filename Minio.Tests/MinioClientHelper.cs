using Minio.DataModel;

namespace Minio.Tests
{
	/// <summary>
	/// Helper for <see cref="MinioClient"/>
	/// </summary>
	public static class MinioClientHelper
	{
		/// <summary>
		/// Get IAM policy for assume role.
		/// Restricts access by <paramref name="bucketName"/> and <paramref name="pathPrefix"/>.
		/// Additionally, blocks modification operations if <paramref name="readOnly"/> is <c>true</c>.
		/// </summary>
		public static string GetBucketPolicy(
			string bucketName,
			string pathPrefix = null,
			bool readOnly = false)
		{
			var actionJson = readOnly
				? "\"s3:GetBucketLocation\",\"s3:GetObject\""
				: "\"s3:*\"";
			var resourceJson = string.IsNullOrEmpty(pathPrefix)
				? $"\"arn:aws:s3:::{bucketName}/*\""
				: $"\"arn:aws:s3:::{bucketName}/{pathPrefix}*\"";

			return
				$"{{\"Version\": \"2012-10-17\",\"Statement\": [{{\"Action\": [{actionJson}], \"Effect\": \"Allow\", \"Resource\": [{resourceJson}]}}]}}";
		}

		/// <summary>
		/// Updates client credentials.
		/// </summary>
		public static MinioClient WithCredentials(
			this MinioClient minioClient,
			AssumeRoleResult assumeRoleResult) =>
			minioClient.WithCredentials(
				assumeRoleResult.Credentials.AccessKeyId,
				assumeRoleResult.Credentials.SecretAccessKey,
				assumeRoleResult.Credentials.SessionToken);
	}
}
