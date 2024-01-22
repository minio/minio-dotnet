﻿/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017-2021 MinIO, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Minio.DataModel;
using Minio.Exceptions;
#if !NET6_0_OR_GREATER
using System.Collections.Concurrent;
#endif

namespace Minio.Helper;

public static class Utils
{
    // We support '.' with bucket names but we fallback to using path
    // style requests instead for such buckets.
    private static readonly Regex validBucketName =
        new("^[a-z0-9][a-z0-9\\.\\-]{1,61}[a-z0-9]$", RegexOptions.None, TimeSpan.FromHours(1));

    // Invalid bucket name with double dot.
    private static readonly Regex invalidDotBucketName = new("`/./.", RegexOptions.None, TimeSpan.FromHours(1));

    private static readonly Lazy<IDictionary<string, string>> contentTypeMap = new(AddContentTypeMappings);

    /// <summary>
    ///     IsValidBucketName - verify bucket name in accordance with
    ///     http://docs.aws.amazon.com/AmazonS3/latest/dev/UsingBucket.html
    /// </summary>
    /// <param name="bucketName">Bucket to test existence of</param>
    internal static void ValidateBucketName(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName))
            throw new InvalidBucketNameException(bucketName, "Bucket name cannot be empty.");
        if (bucketName.Length < 3)
            throw new InvalidBucketNameException(bucketName, "Bucket name cannot be smaller than 3 characters.");
        if (bucketName.Length > 63)
            throw new InvalidBucketNameException(bucketName, "Bucket name cannot be greater than 63 characters.");
        if (bucketName[0] == '.' || bucketName[^1] == '.')
            throw new InvalidBucketNameException(bucketName, "Bucket name cannot start or end with a '.' dot.");
        if (bucketName.Any(char.IsUpper))
            throw new InvalidBucketNameException(bucketName, "Bucket name cannot have upper case characters");
        if (invalidDotBucketName.IsMatch(bucketName))
            throw new InvalidBucketNameException(bucketName, "Bucket name cannot have successive periods.");
        if (!validBucketName.IsMatch(bucketName))
            throw new InvalidBucketNameException(bucketName, "Bucket name contains invalid characters.");
    }

    // IsValidObjectName - verify object name in accordance with
    // http://docs.aws.amazon.com/AmazonS3/latest/dev/UsingMetadata.html
    internal static void ValidateObjectName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(objectName.Trim()))
            throw new InvalidObjectNameException(objectName, "Object name cannot be empty.");

        // c# strings are in utf16 format. they are already in unicode format when they arrive here.
        if (objectName.Length > 512)
            throw new InvalidObjectNameException(objectName, "Object name cannot be greater than 1024 characters.");
    }

    internal static void ValidateObjectPrefix(string objectPrefix)
    {
        if (objectPrefix.Length > 512)
            throw new InvalidObjectPrefixException(objectPrefix,
                "Object prefix cannot be greater than 1024 characters.");
    }

    // Return url encoded string where reserved characters have been percent-encoded
    internal static string UrlEncode(string input)
    {
        // The following characters are not allowed on the server side
        // '-', '_', '.', '/', '*'
        return Uri.EscapeDataString(input).Replace("\\!", "%21", StringComparison.Ordinal)
            .Replace("\\\"", "%22", StringComparison.Ordinal)
            .Replace("\\#", "%23", StringComparison.Ordinal)
            .Replace("\\$", "%24", StringComparison.Ordinal)
            .Replace("\\%", "%25", StringComparison.Ordinal)
            .Replace("\\&", "%26", StringComparison.Ordinal)
            .Replace("\\'", "%27", StringComparison.Ordinal)
            .Replace("\\(", "%28", StringComparison.Ordinal)
            .Replace("\\)", "%29", StringComparison.Ordinal)
            .Replace("\\+", "%2B", StringComparison.Ordinal)
            .Replace("\\,", "%2C", StringComparison.Ordinal)
            .Replace("\\:", "%3A", StringComparison.Ordinal)
            .Replace("\\;", "%3B", StringComparison.Ordinal)
            .Replace("\\<", "%3C", StringComparison.Ordinal)
            .Replace("\\=", "%3D", StringComparison.Ordinal)
            .Replace("\\>", "%3E", StringComparison.Ordinal)
            .Replace("\\?", "%3F", StringComparison.Ordinal)
            .Replace("\\@", "%40", StringComparison.Ordinal)
            .Replace("\\[", "%5B", StringComparison.Ordinal)
            .Replace("\\\\", "%5C", StringComparison.Ordinal)
            .Replace("\\]", "%5D", StringComparison.Ordinal)
            .Replace("\\^", "%5E", StringComparison.Ordinal)
            .Replace("\\'", "%60", StringComparison.Ordinal)
            .Replace("\\{", "%7B", StringComparison.Ordinal)
            .Replace("\\|", "%7C", StringComparison.Ordinal)
            .Replace("\\}", "%7D", StringComparison.Ordinal)
            .Replace("\\~", "%7E", StringComparison.Ordinal);
    }

    // Return encoded path where extra "/" are trimmed off.
    internal static string EncodePath(string path)
    {
        var encodedPathBuf = new StringBuilder();
        foreach (var pathSegment in path.Split('/'))
            if (pathSegment.Length != 0)
            {
                if (encodedPathBuf.Length > 0) _ = encodedPathBuf.Append('/');
                _ = encodedPathBuf.Append(UrlEncode(pathSegment));
            }

        if (path.StartsWith("/", StringComparison.OrdinalIgnoreCase)) _ = encodedPathBuf.Insert(0, '/');
        if (path.EndsWith("/", StringComparison.OrdinalIgnoreCase)) _ = encodedPathBuf.Append('/');
        return encodedPathBuf.ToString();
    }

    internal static bool IsAnonymousClient(string accessKey, string secretKey)
    {
        return string.IsNullOrEmpty(secretKey) && string.IsNullOrEmpty(accessKey);
    }

    internal static void ValidateFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("empty file name is not allowed", nameof(filePath));

        var fileName = Path.GetFileName(filePath);
        var fileExists = File.Exists(filePath);
        if (fileExists)
        {
            var attr = File.GetAttributes(filePath);
            if (attr.HasFlag(FileAttributes.Directory))
                throw new ArgumentException($"'{fileName}': not a regular file", nameof(filePath));
        }
    }

    internal static string GetContentType(string fileName)
    {
        string extension = null;
        try
        {
            extension = Path.GetExtension(fileName);
        }
        catch
        {
        }

        if (string.IsNullOrEmpty(extension)) return "application/octet-stream";

        return contentTypeMap.Value.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    public static void MoveWithReplace(string sourceFileName, string destFileName)
    {
        try
        {
            // first, delete target file if exists, as File.Move() does not support overwrite
            if (File.Exists(destFileName)) File.Delete(destFileName);

            File.Move(sourceFileName, destFileName);
        }
        catch
        {
        }
    }

    internal static bool IsSupersetOf(IList<string> l1, IList<string> l2)
    {
        if (l2 is null) return true;

        if (l1 is null) return false;

        return !l2.Except(l1, StringComparer.Ordinal).Any();
    }

    public static async Task ForEachAsync<TSource>(this IEnumerable<TSource> source, bool runInParallel = false,
        int maxNoOfParallelProcesses = 4) where TSource : Task
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        try
        {
            if (runInParallel)
            {
#if NET6_0_OR_GREATER
                ParallelOptions parallelOptions = new()
                {
                    MaxDegreeOfParallelism
                        = maxNoOfParallelProcesses
                };
                await Parallel.ForEachAsync(source, parallelOptions,
                    async (task, cancellationToken) => await task.ConfigureAwait(false)).ConfigureAwait(false);
#else
                await Task.WhenAll(Partitioner.Create(source).GetPartitions(maxNoOfParallelProcesses)
                    .Select(partition => Task.Run(async delegate
                        {
#pragma warning disable IDISP007 // Don't dispose injected
                            using (partition)
                            {
                                while (partition.MoveNext())
                                    await partition.Current.ConfigureAwait(false);
                            }
#pragma warning restore IDISP007 // Don't dispose injected
                        }
                    ))).ConfigureAwait(false);
#endif
            }
            else
            {
                foreach (var task in source) await task.ConfigureAwait(false);
            }
        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.Flatten().InnerExceptions)
                // Handle or log the individual exception 'ex'
                Console.WriteLine($"Exception occurred: {ex.Message}");
        }
    }

    public static bool CaseInsensitiveContains(string text, string value,
        StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException($"'{nameof(text)}' cannot be null or empty.", nameof(text));

        return text.Contains(value, stringComparison);
    }

    /// <summary>
    ///     Calculate part size and number of parts required.
    /// </summary>
    /// <param name="size"></param>
    /// <param name="copy"> If true, use COPY part size, else use PUT part size</param>
    /// <returns></returns>
    public static MultiPartInfo CalculateMultiPartSize(long size, bool copy = false)
    {
        if (size == -1) size = Constants.MaximumStreamObjectSize;

        if (size > Constants.MaxMultipartPutObjectSize)
            throw new EntityTooLargeException(
                $"Your proposed upload size {size} exceeds the maximum allowed object size {Constants.MaxMultipartPutObjectSize}");

        var partSize = (double)Math.Ceiling((decimal)size / Constants.MaxParts);
        var minPartSize = copy ? Constants.MinimumCOPYPartSize : Constants.MinimumPUTPartSize;
        partSize = (double)Math.Ceiling((decimal)partSize / minPartSize) * minPartSize;
        var partCount = Math.Ceiling(size / partSize);
        var lastPartSize = size - ((partCount - 1) * partSize);

        return new MultiPartInfo { PartSize = partSize, PartCount = partCount, LastPartSize = lastPartSize };
    }

    /// <summary>
    ///     Check if input expires value is valid.
    /// </summary>
    /// <param name="expiryInt">time to expiry in seconds</param>
    /// <returns>bool</returns>
    public static bool IsValidExpiry(int expiryInt)
    {
        return expiryInt > 0 && expiryInt <= Constants.DefaultExpiryTime;
    }

    internal static string GetMD5SumStr(ReadOnlySpan<byte> key)
    {
#if NETSTANDARD
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
        using var md5
            = MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
        var hashedBytes
            = md5.ComputeHash(key.ToArray());
#else
        ReadOnlySpan<byte> hashedBytes = MD5.HashData(key);
#endif
        return Convert.ToBase64String(hashedBytes);
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "One time list of type mappings")]
    private static Dictionary<string, string> AddContentTypeMappings()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".323", "text/h323" },
            { ".3g2", "video/3gpp2" },
            { ".3gp", "video/3gpp" },
            { ".3gp2", "video/3gpp2" },
            { ".3gpp", "video/3gpp" },
            { ".7z", "application/x-7z-compressed" },
            { ".aa", "audio/audible" },
            { ".AAC", "audio/aac" },
            { ".aax", "audio/vnd.audible.aax" },
            { ".ac3", "audio/ac3" },
            { ".accda", "application/msaccess.addin" },
            { ".accdb", "application/msaccess" },
            { ".accdc", "application/msaccess.cab" },
            { ".accde", "application/msaccess" },
            { ".accdr", "application/msaccess.runtime" },
            { ".accdt", "application/msaccess" },
            { ".accdw", "application/msaccess.webapplication" },
            { ".accft", "application/msaccess.ftemplate" },
            { ".acx", "application/internet-property-stream" },
            { ".AddIn", "text/xml" },
            { ".ade", "application/msaccess" },
            { ".adobebridge", "application/x-bridge-url" },
            { ".adp", "application/msaccess" },
            { ".ADT", "audio/vnd.dlna.adts" },
            { ".ADTS", "audio/aac" },
            { ".ai", "application/postscript" },
            { ".aif", "audio/aiff" },
            { ".aifc", "audio/aiff" },
            { ".aiff", "audio/aiff" },
            { ".air", "application/vnd.adobe.air-application-installer-package+zip" },
            { ".amc", "application/mpeg" },
            { ".anx", "application/annodex" },
            { ".apk", "application/vnd.android.package-archive" },
            { ".application", "application/x-ms-application" },
            { ".art", "image/x-jg" },
            { ".asa", "application/xml" },
            { ".asax", "application/xml" },
            { ".ascx", "application/xml" },
            { ".asf", "video/x-ms-asf" },
            { ".ashx", "application/xml" },
            { ".asm", "text/plain" },
            { ".asmx", "application/xml" },
            { ".aspx", "application/xml" },
            { ".asr", "video/x-ms-asf" },
            { ".asx", "video/x-ms-asf" },
            { ".atom", "application/atom+xml" },
            { ".au", "audio/basic" },
            { ".avi", "video/x-msvideo" },
            { ".axa", "audio/annodex" },
            { ".axs", "application/olescript" },
            { ".axv", "video/annodex" },
            { ".bas", "text/plain" },
            { ".bcpio", "application/x-bcpio" },
            { ".bmp", "image/bmp" },
            { ".c", "text/plain" },
            { ".caf", "audio/x-caf" },
            { ".calx", "application/vnd.ms-office.calx" },
            { ".cat", "application/vnd.ms-pki.seccat" },
            { ".cc", "text/plain" },
            { ".cd", "text/plain" },
            { ".cdda", "audio/aiff" },
            { ".cdf", "application/x-cdf" },
            { ".cer", "application/x-x509-ca-cert" },
            { ".cfg", "text/plain" },
            { ".class", "application/x-java-applet" },
            { ".clp", "application/x-msclip" },
            { ".cmd", "text/plain" },
            { ".cmx", "image/x-cmx" },
            { ".cnf", "text/plain" },
            { ".cod", "image/cis-cod" },
            { ".config", "application/xml" },
            { ".contact", "text/x-ms-contact" },
            { ".coverage", "application/xml" },
            { ".cpio", "application/x-cpio" },
            { ".cpp", "text/plain" },
            { ".crd", "application/x-mscardfile" },
            { ".crl", "application/pkix-crl" },
            { ".crt", "application/x-x509-ca-cert" },
            { ".cs", "text/plain" },
            { ".csdproj", "text/plain" },
            { ".csh", "application/x-csh" },
            { ".csproj", "text/plain" },
            { ".css", "text/css" },
            { ".csv", "text/csv" },
            { ".cxx", "text/plain" },
            { ".datasource", "application/xml" },
            { ".dbproj", "text/plain" },
            { ".dcr", "application/x-director" },
            { ".def", "text/plain" },
            { ".der", "application/x-x509-ca-cert" },
            { ".dgml", "application/xml" },
            { ".dib", "image/bmp" },
            { ".dif", "video/x-dv" },
            { ".dir", "application/x-director" },
            { ".disco", "text/xml" },
            { ".divx", "video/divx" },
            { ".dll", "application/x-msdownload" },
            { ".dll.config", "text/xml" },
            { ".dlm", "text/dlm" },
            { ".doc", "application/msword" },
            { ".docm", "application/vnd.ms-word.document.macroEnabled.12" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".dot", "application/msword" },
            { ".dotm", "application/vnd.ms-word.template.macroEnabled.12" },
            { ".dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template" },
            { ".dsw", "text/plain" },
            { ".dtd", "text/xml" },
            { ".dtsConfig", "text/xml" },
            { ".dv", "video/x-dv" },
            { ".dvi", "application/x-dvi" },
            { ".dwf", "drawing/x-dwf" },
            { ".dwg", "application/acad" },
            { ".dxf", "application/x-dxf" },
            { ".dxr", "application/x-director" },
            { ".eml", "message/rfc822" },
            { ".eot", "application/vnd.ms-fontobject" },
            { ".eps", "application/postscript" },
            { ".etl", "application/etl" },
            { ".etx", "text/x-setext" },
            { ".evy", "application/envoy" },
            { ".exe.config", "text/xml" },
            { ".fdf", "application/vnd.fdf" },
            { ".fif", "application/fractals" },
            { ".filters", "application/xml" },
            { ".flac", "audio/flac" },
            { ".flr", "x-world/x-vrml" },
            { ".flv", "video/x-flv" },
            { ".fsscript", "application/fsharp-script" },
            { ".fsx", "application/fsharp-script" },
            { ".generictest", "application/xml" },
            { ".gif", "image/gif" },
            { ".gpx", "application/gpx+xml" },
            { ".group", "text/x-ms-group" },
            { ".gsm", "audio/x-gsm" },
            { ".gtar", "application/x-gtar" },
            { ".gz", "application/x-gzip" },
            { ".h", "text/plain" },
            { ".hdf", "application/x-hdf" },
            { ".hdml", "text/x-hdml" },
            { ".hhc", "application/x-oleobject" },
            { ".hlp", "application/winhlp" },
            { ".hpp", "text/plain" },
            { ".hqx", "application/mac-binhex40" },
            { ".hta", "application/hta" },
            { ".htc", "text/x-component" },
            { ".htm", "text/html" },
            { ".html", "text/html" },
            { ".htt", "text/webviewhtml" },
            { ".hxa", "application/xml" },
            { ".hxc", "application/xml" },
            { ".hxe", "application/xml" },
            { ".hxf", "application/xml" },
            { ".hxk", "application/xml" },
            { ".hxt", "text/html" },
            { ".hxv", "application/xml" },
            { ".hxx", "text/plain" },
            { ".i", "text/plain" },
            { ".ico", "image/x-icon" },
            { ".idl", "text/plain" },
            { ".ief", "image/ief" },
            { ".iii", "application/x-iphone" },
            { ".inc", "text/plain" },
            { ".ini", "text/plain" },
            { ".inl", "text/plain" },
            { ".ins", "application/x-internet-signup" },
            { ".ipa", "application/x-itunes-ipa" },
            { ".ipg", "application/x-itunes-ipg" },
            { ".ipproj", "text/plain" },
            { ".ipsw", "application/x-itunes-ipsw" },
            { ".iqy", "text/x-ms-iqy" },
            { ".isp", "application/x-internet-signup" },
            { ".ite", "application/x-itunes-ite" },
            { ".itlp", "application/x-itunes-itlp" },
            { ".itms", "application/x-itunes-itms" },
            { ".itpc", "application/x-itunes-itpc" },
            { ".IVF", "video/x-ivf" },
            { ".jar", "application/java-archive" },
            { ".jck", "application/liquidmotion" },
            { ".jcz", "application/liquidmotion" },
            { ".jfif", "image/pjpeg" },
            { ".jnlp", "application/x-java-jnlp-file" },
            { ".jpe", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".jpg", "image/jpeg" },
            { ".js", "application/javascript" },
            { ".json", "application/json" },
            { ".jsx", "text/jscript" },
            { ".jsxbin", "text/plain" },
            { ".latex", "application/x-latex" },
            { ".library-ms", "application/windows-library+xml" },
            { ".lit", "application/x-ms-reader" },
            { ".loadtest", "application/xml" },
            { ".lsf", "video/x-la-asf" },
            { ".lst", "text/plain" },
            { ".lsx", "video/x-la-asf" },
            { ".m13", "application/x-msmediaview" },
            { ".m14", "application/x-msmediaview" },
            { ".m1v", "video/mpeg" },
            { ".m2t", "video/vnd.dlna.mpeg-tts" },
            { ".m2ts", "video/vnd.dlna.mpeg-tts" },
            { ".m2v", "video/mpeg" },
            { ".m3u", "audio/x-mpegurl" },
            { ".m3u8", "audio/x-mpegurl" },
            { ".m4a", "audio/m4a" },
            { ".m4b", "audio/m4b" },
            { ".m4p", "audio/m4p" },
            { ".m4r", "audio/x-m4r" },
            { ".m4v", "video/x-m4v" },
            { ".mac", "image/x-macpaint" },
            { ".mak", "text/plain" },
            { ".man", "application/x-troff-man" },
            { ".manifest", "application/x-ms-manifest" },
            { ".map", "text/plain" },
            { ".master", "application/xml" },
            { ".mbox", "application/mbox" },
            { ".mda", "application/msaccess" },
            { ".mdb", "application/x-msaccess" },
            { ".mde", "application/msaccess" },
            { ".me", "application/x-troff-me" },
            { ".mfp", "application/x-shockwave-flash" },
            { ".mht", "message/rfc822" },
            { ".mhtml", "message/rfc822" },
            { ".mid", "audio/mid" },
            { ".midi", "audio/mid" },
            { ".mk", "text/plain" },
            { ".mmf", "application/x-smaf" },
            { ".mno", "text/xml" },
            { ".mny", "application/x-msmoney" },
            { ".mod", "video/mpeg" },
            { ".mov", "video/quicktime" },
            { ".movie", "video/x-sgi-movie" },
            { ".mp2", "video/mpeg" },
            { ".mp2v", "video/mpeg" },
            { ".mp3", "audio/mpeg" },
            { ".mp4", "video/mp4" },
            { ".mp4v", "video/mp4" },
            { ".mpa", "video/mpeg" },
            { ".mpe", "video/mpeg" },
            { ".mpeg", "video/mpeg" },
            { ".mpf", "application/vnd.ms-mediapackage" },
            { ".mpg", "video/mpeg" },
            { ".mpp", "application/vnd.ms-project" },
            { ".mpv2", "video/mpeg" },
            { ".mqv", "video/quicktime" },
            { ".ms", "application/x-troff-ms" },
            { ".msg", "application/vnd.ms-outlook" },
            { ".mts", "video/vnd.dlna.mpeg-tts" },
            { ".mtx", "application/xml" },
            { ".mvb", "application/x-msmediaview" },
            { ".mvc", "application/x-miva-compiled" },
            { ".mxp", "application/x-mmxp" },
            { ".nc", "application/x-netcdf" },
            { ".nsc", "video/x-ms-asf" },
            { ".nws", "message/rfc822" },
            { ".oda", "application/oda" },
            { ".odb", "application/vnd.oasis.opendocument.database" },
            { ".odc", "application/vnd.oasis.opendocument.chart" },
            { ".odf", "application/vnd.oasis.opendocument.formula" },
            { ".odg", "application/vnd.oasis.opendocument.graphics" },
            { ".odh", "text/plain" },
            { ".odi", "application/vnd.oasis.opendocument.image" },
            { ".odl", "text/plain" },
            { ".odm", "application/vnd.oasis.opendocument.text-master" },
            { ".odp", "application/vnd.oasis.opendocument.presentation" },
            { ".ods", "application/vnd.oasis.opendocument.spreadsheet" },
            { ".odt", "application/vnd.oasis.opendocument.text" },
            { ".oga", "audio/ogg" },
            { ".ogg", "audio/ogg" },
            { ".ogv", "video/ogg" },
            { ".ogx", "application/ogg" },
            { ".one", "application/onenote" },
            { ".onea", "application/onenote" },
            { ".onepkg", "application/onenote" },
            { ".onetmp", "application/onenote" },
            { ".onetoc", "application/onenote" },
            { ".onetoc2", "application/onenote" },
            { ".opus", "audio/ogg" },
            { ".orderedtest", "application/xml" },
            { ".osdx", "application/opensearchdescription+xml" },
            { ".otf", "application/font-sfnt" },
            { ".otg", "application/vnd.oasis.opendocument.graphics-template" },
            { ".oth", "application/vnd.oasis.opendocument.text-web" },
            { ".otp", "application/vnd.oasis.opendocument.presentation-template" },
            { ".ots", "application/vnd.oasis.opendocument.spreadsheet-template" },
            { ".ott", "application/vnd.oasis.opendocument.text-template" },
            { ".oxt", "application/vnd.openofficeorg.extension" },
            { ".p10", "application/pkcs10" },
            { ".p12", "application/x-pkcs12" },
            { ".p7b", "application/x-pkcs7-certificates" },
            { ".p7c", "application/pkcs7-mime" },
            { ".p7m", "application/pkcs7-mime" },
            { ".p7r", "application/x-pkcs7-certreqresp" },
            { ".p7s", "application/pkcs7-signature" },
            { ".pbm", "image/x-portable-bitmap" },
            { ".pcast", "application/x-podcast" },
            { ".pct", "image/pict" },
            { ".pdf", "application/pdf" },
            { ".pfx", "application/x-pkcs12" },
            { ".pgm", "image/x-portable-graymap" },
            { ".pic", "image/pict" },
            { ".pict", "image/pict" },
            { ".pkgdef", "text/plain" },
            { ".pkgundef", "text/plain" },
            { ".pko", "application/vnd.ms-pki.pko" },
            { ".pls", "audio/scpls" },
            { ".pma", "application/x-perfmon" },
            { ".pmc", "application/x-perfmon" },
            { ".pml", "application/x-perfmon" },
            { ".pmr", "application/x-perfmon" },
            { ".pmw", "application/x-perfmon" },
            { ".png", "image/png" },
            { ".pnm", "image/x-portable-anymap" },
            { ".pnt", "image/x-macpaint" },
            { ".pntg", "image/x-macpaint" },
            { ".pnz", "image/png" },
            { ".pot", "application/vnd.ms-powerpoint" },
            { ".potm", "application/vnd.ms-powerpoint.template.macroEnabled.12" },
            { ".potx", "application/vnd.openxmlformats-officedocument.presentationml.template" },
            { ".ppa", "application/vnd.ms-powerpoint" },
            { ".ppam", "application/vnd.ms-powerpoint.addin.macroEnabled.12" },
            { ".ppm", "image/x-portable-pixmap" },
            { ".pps", "application/vnd.ms-powerpoint" },
            { ".ppsm", "application/vnd.ms-powerpoint.slideshow.macroEnabled.12" },
            { ".ppsx", "application/vnd.openxmlformats-officedocument.presentationml.slideshow" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptm", "application/vnd.ms-powerpoint.presentation.macroEnabled.12" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".prf", "application/pics-rules" },
            { ".ps", "application/postscript" },
            { ".psc1", "application/PowerShell" },
            { ".psess", "application/xml" },
            { ".pst", "application/vnd.ms-outlook" },
            { ".pub", "application/x-mspublisher" },
            { ".pwz", "application/vnd.ms-powerpoint" },
            { ".qht", "text/x-html-insertion" },
            { ".qhtm", "text/x-html-insertion" },
            { ".qt", "video/quicktime" },
            { ".qti", "image/x-quicktime" },
            { ".qtif", "image/x-quicktime" },
            { ".qtl", "application/x-quicktimeplayer" },
            { ".ra", "audio/x-pn-realaudio" },
            { ".ram", "audio/x-pn-realaudio" },
            { ".rar", "application/x-rar-compressed" },
            { ".ras", "image/x-cmu-raster" },
            { ".rat", "application/rat-file" },
            { ".rc", "text/plain" },
            { ".rc2", "text/plain" },
            { ".rct", "text/plain" },
            { ".rdlc", "application/xml" },
            { ".reg", "text/plain" },
            { ".resx", "application/xml" },
            { ".rf", "image/vnd.rn-realflash" },
            { ".rgb", "image/x-rgb" },
            { ".rgs", "text/plain" },
            { ".rm", "application/vnd.rn-realmedia" },
            { ".rmi", "audio/mid" },
            { ".rmp", "application/vnd.rn-rn_music_package" },
            { ".roff", "application/x-troff" },
            { ".rpm", "audio/x-pn-realaudio-plugin" },
            { ".rqy", "text/x-ms-rqy" },
            { ".rtf", "application/rtf" },
            { ".rtx", "text/richtext" },
            { ".ruleset", "application/xml" },
            { ".s", "text/plain" },
            { ".safariextz", "application/x-safari-safariextz" },
            { ".scd", "application/x-msschedule" },
            { ".scr", "text/plain" },
            { ".sct", "text/scriptlet" },
            { ".sd2", "audio/x-sd2" },
            { ".sdp", "application/sdp" },
            { ".searchConnector-ms", "application/windows-search-connector+xml" },
            { ".setpay", "application/set-payment-initiation" },
            { ".setreg", "application/set-registration-initiation" },
            { ".settings", "application/xml" },
            { ".sgimb", "application/x-sgimb" },
            { ".sgml", "text/sgml" },
            { ".sh", "application/x-sh" },
            { ".shar", "application/x-shar" },
            { ".shtml", "text/html" },
            { ".sit", "application/x-stuffit" },
            { ".sitemap", "application/xml" },
            { ".skin", "application/xml" },
            { ".skp", "application/x-koan" },
            { ".sldm", "application/vnd.ms-powerpoint.slide.macroEnabled.12" },
            { ".sldx", "application/vnd.openxmlformats-officedocument.presentationml.slide" },
            { ".slk", "application/vnd.ms-excel" },
            { ".sln", "text/plain" },
            { ".slupkg-ms", "application/x-ms-license" },
            { ".smd", "audio/x-smd" },
            { ".smx", "audio/x-smd" },
            { ".smz", "audio/x-smd" },
            { ".snd", "audio/basic" },
            { ".snippet", "application/xml" },
            { ".sol", "text/plain" },
            { ".sor", "text/plain" },
            { ".spc", "application/x-pkcs7-certificates" },
            { ".spl", "application/futuresplash" },
            { ".spx", "audio/ogg" },
            { ".src", "application/x-wais-source" },
            { ".srf", "text/plain" },
            { ".SSISDeploymentManifest", "text/xml" },
            { ".ssm", "application/streamingmedia" },
            { ".sst", "application/vnd.ms-pki.certstore" },
            { ".stl", "application/vnd.ms-pki.stl" },
            { ".sv4cpio", "application/x-sv4cpio" },
            { ".sv4crc", "application/x-sv4crc" },
            { ".svc", "application/xml" },
            { ".svg", "image/svg+xml" },
            { ".swf", "application/x-shockwave-flash" },
            { ".step", "application/step" },
            { ".stp", "application/step" },
            { ".t", "application/x-troff" },
            { ".tar", "application/x-tar" },
            { ".tcl", "application/x-tcl" },
            { ".testrunconfig", "application/xml" },
            { ".testsettings", "application/xml" },
            { ".tex", "application/x-tex" },
            { ".texi", "application/x-texinfo" },
            { ".texinfo", "application/x-texinfo" },
            { ".tgz", "application/x-compressed" },
            { ".thmx", "application/vnd.ms-officetheme" },
            { ".tif", "image/tiff" },
            { ".tiff", "image/tiff" },
            { ".tlh", "text/plain" },
            { ".tli", "text/plain" },
            { ".tr", "application/x-troff" },
            { ".trm", "application/x-msterminal" },
            { ".trx", "application/xml" },
            { ".ts", "video/vnd.dlna.mpeg-tts" },
            { ".tsv", "text/tab-separated-values" },
            { ".ttf", "application/font-sfnt" },
            { ".tts", "video/vnd.dlna.mpeg-tts" },
            { ".txt", "text/plain" },
            { ".uls", "text/iuls" },
            { ".user", "text/plain" },
            { ".ustar", "application/x-ustar" },
            { ".vb", "text/plain" },
            { ".vbdproj", "text/plain" },
            { ".vbk", "video/mpeg" },
            { ".vbproj", "text/plain" },
            { ".vbs", "text/vbscript" },
            { ".vcf", "text/x-vcard" },
            { ".vcproj", "application/xml" },
            { ".vcs", "text/plain" },
            { ".vcxproj", "application/xml" },
            { ".vddproj", "text/plain" },
            { ".vdp", "text/plain" },
            { ".vdproj", "text/plain" },
            { ".vdx", "application/vnd.ms-visio.viewer" },
            { ".vml", "text/xml" },
            { ".vscontent", "application/xml" },
            { ".vsct", "text/xml" },
            { ".vsd", "application/vnd.visio" },
            { ".vsi", "application/ms-vsi" },
            { ".vsix", "application/vsix" },
            { ".vsixlangpack", "text/xml" },
            { ".vsixmanifest", "text/xml" },
            { ".vsmdi", "application/xml" },
            { ".vspscc", "text/plain" },
            { ".vss", "application/vnd.visio" },
            { ".vsscc", "text/plain" },
            { ".vssettings", "text/xml" },
            { ".vssscc", "text/plain" },
            { ".vst", "application/vnd.visio" },
            { ".vstemplate", "text/xml" },
            { ".vsto", "application/x-ms-vsto" },
            { ".vsw", "application/vnd.visio" },
            { ".vsx", "application/vnd.visio" },
            { ".vtx", "application/vnd.visio" },
            { ".wav", "audio/wav" },
            { ".wave", "audio/wav" },
            { ".wax", "audio/x-ms-wax" },
            { ".wbk", "application/msword" },
            { ".wbmp", "image/vnd.wap.wbmp" },
            { ".wcm", "application/vnd.ms-works" },
            { ".wdb", "application/vnd.ms-works" },
            { ".wdp", "image/vnd.ms-photo" },
            { ".webarchive", "application/x-safari-webarchive" },
            { ".webm", "video/webm" },
            { ".webp", "image/webp" },
            { ".webtest", "application/xml" },
            { ".wiq", "application/xml" },
            { ".wiz", "application/msword" },
            { ".wks", "application/vnd.ms-works" },
            { ".WLMP", "application/wlmoviemaker" },
            { ".wlpginstall", "application/x-wlpg-detect" },
            { ".wlpginstall3", "application/x-wlpg3-detect" },
            { ".wm", "video/x-ms-wm" },
            { ".wma", "audio/x-ms-wma" },
            { ".wmd", "application/x-ms-wmd" },
            { ".wmf", "application/x-msmetafile" },
            { ".wml", "text/vnd.wap.wml" },
            { ".wmlc", "application/vnd.wap.wmlc" },
            { ".wmls", "text/vnd.wap.wmlscript" },
            { ".wmlsc", "application/vnd.wap.wmlscriptc" },
            { ".wmp", "video/x-ms-wmp" },
            { ".wmv", "video/x-ms-wmv" },
            { ".wmx", "video/x-ms-wmx" },
            { ".wmz", "application/x-ms-wmz" },
            { ".woff", "application/font-woff" },
            { ".wpl", "application/vnd.ms-wpl" },
            { ".wps", "application/vnd.ms-works" },
            { ".wri", "application/x-mswrite" },
            { ".wrl", "x-world/x-vrml" },
            { ".wrz", "x-world/x-vrml" },
            { ".wsc", "text/scriptlet" },
            { ".wsdl", "text/xml" },
            { ".wvx", "video/x-ms-wvx" },
            { ".x", "application/directx" },
            { ".xaf", "x-world/x-vrml" },
            { ".xaml", "application/xaml+xml" },
            { ".xap", "application/x-silverlight-app" },
            { ".xbap", "application/x-ms-xbap" },
            { ".xbm", "image/x-xbitmap" },
            { ".xdr", "text/plain" },
            { ".xht", "application/xhtml+xml" },
            { ".xhtml", "application/xhtml+xml" },
            { ".xla", "application/vnd.ms-excel" },
            { ".xlam", "application/vnd.ms-excel.addin.macroEnabled.12" },
            { ".xlc", "application/vnd.ms-excel" },
            { ".xld", "application/vnd.ms-excel" },
            { ".xlk", "application/vnd.ms-excel" },
            { ".xll", "application/vnd.ms-excel" },
            { ".xlm", "application/vnd.ms-excel" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12" },
            { ".xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".xlt", "application/vnd.ms-excel" },
            { ".xltm", "application/vnd.ms-excel.template.macroEnabled.12" },
            { ".xltx", "application/vnd.openxmlformats-officedocument.spreadsheetml.template" },
            { ".xlw", "application/vnd.ms-excel" },
            { ".xml", "text/xml" },
            { ".xmta", "application/xml" },
            { ".xof", "x-world/x-vrml" },
            { ".XOML", "text/plain" },
            { ".xpm", "image/x-xpixmap" },
            { ".xps", "application/vnd.ms-xpsdocument" },
            { ".xrm-ms", "text/xml" },
            { ".xsc", "application/xml" },
            { ".xsd", "text/xml" },
            { ".xsf", "text/xml" },
            { ".xsl", "text/xml" },
            { ".xslt", "text/xml" },
            { ".xss", "application/xml" },
            { ".xspf", "application/xspf+xml" },
            { ".xwd", "image/x-xwindowdump" },
            { ".z", "application/x-compress" },
            { ".zip", "application/zip" }
        };
    }

    public static string MarshalXML(object obj, string nmspc)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        XmlWriter xw = null;

        var str = string.Empty;

        try
        {
            var settings = new XmlWriterSettings { OmitXmlDeclaration = true };
            var ns = new XmlSerializerNamespaces();
            ns.Add("", nmspc);

            using var sw = new StringWriter(CultureInfo.InvariantCulture);

            var xs = new XmlSerializer(obj.GetType());
            using (xw = XmlWriter.Create(sw, settings))
            {
                xs.Serialize(xw, obj, ns);
                xw.Flush();

                str = sw.ToString();
            }
        }
        finally
        {
            xw.Close();
        }

        return str;
    }

    public static string To8601String(DateTime dt)
    {
        return dt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    public static string RemoveNamespaceInXML(string config)
    {
        // We'll need to remove the namespace within the serialized configuration
        const RegexOptions regexOptions =
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace |
            RegexOptions.Multiline;
        var patternToReplace =
            @"<\w+\s+\w+:nil=""true""(\s+xmlns:\w+=""http://www.w3.org/2001/XMLSchema-instance"")?\s*/>";
        const string patternToMatch = @"<\w+\s+xmlns=""http://s3.amazonaws.com/doc/2006-03-01/""\s*>";
        if (Regex.Match(config, patternToMatch, regexOptions, TimeSpan.FromHours(1)).Success)
            patternToReplace = @"xmlns=""http://s3.amazonaws.com/doc/2006-03-01/""\s*";
        return Regex.Replace(
            config,
            patternToReplace,
            string.Empty,
            regexOptions,
            TimeSpan.FromHours(1)
        );
    }

    public static DateTime From8601String(string dt)
    {
        return DateTime.Parse(dt, null, DateTimeStyles.RoundtripKind);
    }

    public static Uri GetBaseUrl(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture,
                    "{0} is the value of the endpoint. It can't be null or empty.", endpoint),
                nameof(endpoint));

        if (endpoint.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            endpoint = endpoint[..^1];
        if (!endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
            !BuilderUtil.IsValidHostnameOrIPAddress(endpoint))
            throw new InvalidEndpointException(
                string.Format(CultureInfo.InvariantCulture, "{0} is invalid hostname.", endpoint), "endpoint");
        string conn_url;
        if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            throw new InvalidEndpointException(
                string.Format(CultureInfo.InvariantCulture,
                    "{0} the value of the endpoint has the scheme (http/https) in it.", endpoint),
                "endpoint");

        var enable_https = Environment.GetEnvironmentVariable("ENABLE_HTTPS");
        var scheme = enable_https?.Equals("1", StringComparison.OrdinalIgnoreCase) == true ? "https://" : "http://";
        conn_url = scheme + endpoint;
        var url = new Uri(conn_url);
        var hostnameOfUri = url.Authority;
        if (!string.IsNullOrWhiteSpace(hostnameOfUri) && !BuilderUtil.IsValidHostnameOrIPAddress(hostnameOfUri))
            throw new InvalidEndpointException(
                string.Format(CultureInfo.InvariantCulture, "{0}, {1} is invalid hostname.", endpoint, hostnameOfUri),
                "endpoint");

        return url;
    }

    internal static HttpRequestMessageBuilder GetEmptyRestRequest(HttpRequestMessageBuilder requestBuilder)
    {
        var serializedBody = JsonSerializer.Serialize("");
        requestBuilder.AddOrUpdateHeaderParameter("application/json; charset=utf-8", serializedBody);
        return requestBuilder;
    }

    // Converts an object to a byte array
    public static ReadOnlyMemory<byte> ObjectToByteArray(object obj)
    {
        switch (obj)
        {
            case null:
            case Memory<byte> memory when memory.IsEmpty:
            case ReadOnlyMemory<byte> readOnlyMemory when readOnlyMemory.IsEmpty:
                return null;
            default:
                return JsonSerializer.SerializeToUtf8Bytes(obj);
        }
    }

    // Print object key properties and their values
    // Added for debugging purposes

    public static void ObjPrint(object obj)
    {
        foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
        {
            var name = descriptor.Name;
            var value = descriptor.GetValue(obj);
            Console.WriteLine($"{name}={value}");
        }
    }

    public static void Print(object obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        foreach (var prop in obj.GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var value = prop.GetValue(obj, Array.Empty<object>());
            if (string.Equals(prop.Name, "Headers", StringComparison.Ordinal))
                PrintDict((Dictionary<string, string>)value);
            else
                Console.WriteLine("DEBUG >>   {0} = {1}", prop.Name, value);
        }

        Console.WriteLine("DEBUG >>   Print is DONE!\n\n");
    }

    public static void PrintDict(IDictionary<string, string> d)
    {
        if (d is not null)
            foreach (var kv in d)
                Console.WriteLine("DEBUG >>             Dictionary({0} => {1})", kv.Key, kv.Value);

        Console.WriteLine("DEBUG >>             Dictionary: Done printing\n");
    }

    public static string DetermineNamespace(XDocument document)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));

        return document.Root.Attributes().FirstOrDefault(attr => attr.IsNamespaceDeclaration)?.Value ?? string.Empty;
    }

    public static string SerializeToXml<T>(T anyobject) where T : class
    {
        if (anyobject is null) throw new ArgumentNullException(nameof(anyobject));

        var xs = new XmlSerializer(anyobject.GetType());
        using var sw = new StringWriter(CultureInfo.InvariantCulture);
        using var xw = XmlWriter.Create(sw);

        xs.Serialize(xw, anyobject);
        xw.Flush();

        return sw.ToString();
    }

    public static T DeserializeXml<T>(Stream stream) where T : class, new()
    {
        if (stream == null || stream.Length == 0) return default;

        var ns = GetNamespace<T>();
        if (!string.IsNullOrWhiteSpace(ns) && string.Equals(ns, "http://s3.amazonaws.com/doc/2006-03-01/",
                StringComparison.OrdinalIgnoreCase))
        {
            using var amazonAwsS3XmlReader = new AmazonAwsS3XmlReader(stream);
            return (T)new XmlSerializer(typeof(T)).Deserialize(amazonAwsS3XmlReader);
        }

        using var reader = new StreamReader(stream);
        var xmlContent = reader.ReadToEnd();

        return DeserializeXml<T>(xmlContent); // Call the string overload
    }

    public static T DeserializeXml<T>(string xml) where T : class, new()
    {
        if (string.IsNullOrEmpty(xml)) return default;

        var settings = new XmlReaderSettings
        {
            // Disable DTD processing
            DtdProcessing = DtdProcessing.Prohibit,
            // Disable XML schema validation
            XmlResolver = null
        };

        var xRoot = (XmlRootAttribute)typeof(T).GetCustomAttributes(typeof(XmlRootAttribute), true).FirstOrDefault();

        var serializer = new XmlSerializer(typeof(T), xRoot);

        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(stringReader, settings);
        if (xml.Contains("<Error><Code>", StringComparison.Ordinal))
        {
            // Skip the first line
            xml = xml[(xml.IndexOf('\n', StringComparison.Ordinal) + 1)..];
            stringReader.Dispose();
            using var stringReader1 = new StringReader(xml);
            xRoot = new XmlRootAttribute { ElementName = "Error", IsNullable = true };
            serializer = new XmlSerializer(typeof(T), xRoot);
            return (T)serializer.Deserialize(new NamespaceIgnorantXmlTextReader(stringReader1));
        }

        return (T)serializer.Deserialize(xmlReader);
    }

    private static string GetNamespace<T>()
    {
        return typeof(T).GetCustomAttributes(typeof(XmlRootAttribute), true)
            .FirstOrDefault() is XmlRootAttribute xmlRootAttribute
            ? xmlRootAttribute.Namespace
            : null;
    }

    // Class to ignore namespaces when de-serializing
    public class NamespaceIgnorantXmlTextReader : XmlTextReader
    {
        public NamespaceIgnorantXmlTextReader(TextReader reader) : base(reader) { }

        public override string NamespaceURI => string.Empty;
    }
}
