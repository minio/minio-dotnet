namespace Minio.Model;

/// <summary>
/// The result of generating a presigned POST policy.
/// Contains the upload URL and the form fields required for a browser-based multipart/form-data upload.
/// </summary>
public record PostPolicyResult(Uri Url, IDictionary<string, string> Fields);
