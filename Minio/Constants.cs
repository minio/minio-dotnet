using System.Xml.Linq;

namespace Minio;

/// <summary>
/// Shared constants used throughout the MinIO SDK.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The XML namespace for the Amazon S3 service, as defined by the S3 REST API schema
    /// (<c>http://s3.amazonaws.com/doc/2006-03-01/</c>).
    /// Used when parsing S3-compatible XML responses.
    /// </summary>
    public static readonly XNamespace S3Ns = "http://s3.amazonaws.com/doc/2006-03-01/";
}
