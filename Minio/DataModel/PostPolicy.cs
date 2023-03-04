/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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

using System.Text;

namespace Minio.DataModel;

public class PostPolicy
{
    public readonly Dictionary<string, string> formData = new();

    public IList<IList<(string, string, string)>> conditions = new List<IList<(string, string, string)>>();

    public DateTime expiration { get; set; }
    public string Key { get; private set; }
    public string Bucket { get; private set; }

    /// <summary>
    ///     Set expiration policy.
    /// </summary>
    /// <param name="expiration">Expiration time for the policy</param>
    public void SetExpires(DateTime expiration)
    {
        // this.formData.Add("expiration", expiration.ToString());
        this.expiration = expiration;
    }

    /// <summary>
    ///     Set key policy.
    /// </summary>
    /// <param name="key">Object name for the policy</param>
    public void SetKey(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Object key cannot be null or empty", nameof(key));

        conditions.Add(new List<(string, string, string)> { ("eq", "$key", key) });
        // this.formData.Add("key", key);
        Key = key;
    }

    /// <summary>
    ///     Set key prefix policy.
    /// </summary>
    /// <param name="keyStartsWith">Object name prefix for the policy</param>
    public void SetKeyStartsWith(string keyStartsWith)
    {
        if (string.IsNullOrEmpty(keyStartsWith))
            throw new ArgumentException("Object key prefix cannot be null or empty", nameof(keyStartsWith));

        conditions.Add(new List<(string, string, string)> { ("starts-with", "$key", keyStartsWith) });
        // this.formData.Add("key", keyStartsWith);
    }

    /// <summary>
    ///     Set bucket policy.
    /// </summary>
    /// <param name="bucket">Bucket name for the policy</param>
    public void SetBucket(string bucket)
    {
        if (string.IsNullOrEmpty(bucket))
            throw new ArgumentException("Bucket name cannot be null or empty", nameof(bucket));

        conditions.Add(new List<(string, string, string)> { ("eq", "$bucket", bucket) });
        // this.formData.Add("bucket", bucket);
        Bucket = bucket;
    }

    /// <summary>
    ///     Set cache control
    /// </summary>
    /// <param name="cacheControl">CacheControl for the policy</param>
    public void SetCacheControl(string cacheControl)
    {
        if (string.IsNullOrEmpty(cacheControl))
            throw new ArgumentException("Cache-Control argument cannot be null or empty", nameof(cacheControl));

        conditions.Add(new List<(string, string, string)> { ("eq", "$Cache-Control", cacheControl) });
        // this.formData.Add("Cache-Control", cacheControl);
    }

    /// <summary>
    ///     Set content type policy.
    /// </summary>
    /// <param name="contentType">ContentType for the policy</param>
    public void SetContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            throw new ArgumentException("Content-Type argument cannot be null or empty", nameof(contentType));

        conditions.Add(new List<(string, string, string)> { ("eq", "$Content-Type", contentType) });
        // this.formData.Add("Content-Type", contentType);
    }

    /// <summary>
    ///     Set content encoding
    /// </summary>
    /// <param name="contentEncoding">ContentEncoding for the policy</param>
    public void SetContentEncoding(string contentEncoding)
    {
        if (string.IsNullOrEmpty(contentEncoding))
            throw new ArgumentException("Content-Encoding argument cannot be null or empty",
                nameof(contentEncoding));

        conditions.Add(new List<(string, string, string)> { ("eq", "$Content-Encoding", contentEncoding) });
        // this.formData.Add("Content-Encoding", contentEncoding);
    }

    /// <summary>
    ///     Set content length
    /// </summary>
    /// <param name="contentLength">ContentLength for the policy</param>
    public void SetContentLength(long contentLength)
    {
        if (contentLength <= 0) throw new ArgumentException("Negative Content length", nameof(contentLength));

        conditions.Add(new List<(string, string, string)>
            { ("content-length-range", contentLength.ToString(), contentLength.ToString()) });
    }

    /// <summary>
    ///     Set content range
    /// </summary>
    /// <param name="startRange">ContentRange for the policy</param>
    /// <param name="endRange"></param>
    public void SetContentRange(long startRange, long endRange)
    {
        if (startRange < 0 || endRange < 0) throw new ArgumentException("Negative start or end range");

        if (startRange > endRange)
            throw new ArgumentException("Start range is greater than end range", nameof(startRange));

        conditions.Add(new List<(string, string, string)>
            { ("content-length-range", startRange.ToString(), endRange.ToString()) });
    }

    /// <summary>
    ///     Set session token
    /// </summary>
    /// <param name="sessionToken">set session token</param>
    public void SetSessionToken(string sessionToken)
    {
        if (!string.IsNullOrEmpty(sessionToken))
            conditions.Add(
                new List<(string, string, string)> { ("eq", "$x-amz-security-token", sessionToken) });
        // this.formData.Add("x-amz-security-token", sessionToken);
    }

    /// <summary>
    ///     Set the success action status of the object for this policy based upload.
    /// </summary>
    /// <param name="status">Success action status</param>
    public void SetSuccessStatusAction(string status)
    {
        if (string.IsNullOrEmpty(status)) throw new ArgumentException("Status is Empty", nameof(status));

        conditions.Add(new List<(string, string, string)> { ("eq", "$success_action_status", status) });
        // this.formData.Add("success_action_status", status);
    }

    /// <summary>
    ///     Set user specified metadata as a key/value couple.
    /// </summary>
    /// <param name="key">Key and Value to insert in the metadata</param>
    /// <param name="value"></param>
    public void SetUserMetadata(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key is Empty", nameof(key));

        if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value is Empty", nameof(value));

        var headerName = $"x-amz-meta-{key}";
        conditions.Add(new List<(string, string, string)> { ("eq", $"${headerName}", value) });
        formData.Add(headerName, value);
    }

    /// <summary>
    ///     Set signature algorithm policy.
    /// </summary>
    /// <param name="algorithm">Set signature algorithm used for the policy</param>
    public void SetAlgorithm(string algorithm)
    {
        if (string.IsNullOrEmpty(algorithm))
            throw new ArgumentException("Algorithm argument cannot be null or empty", nameof(algorithm));

        conditions.Add(new List<(string, string, string)> { ("eq", "$x-amz-algorithm", algorithm) });
        // this.formData.Add("x-amz-algorithm", algorithm);
    }

    /// <summary>
    ///     Set credential policy.
    /// </summary>
    /// <param name="credential">Set credential string for the policy</param>
    public void SetCredential(string credential)
    {
        if (string.IsNullOrEmpty(credential))
            throw new ArgumentException("credential argument cannot be null or empty", nameof(credential));

        conditions.Add(new List<(string, string, string)> { ("eq", "$x-amz-credential", credential) });
        // this.formData.Add("x-amz-credential", credential);
    }

    /// <summary>
    ///     Set date policy.
    /// </summary>
    /// <param name="date">Set date for the policy</param>
    public void SetDate(DateTime date)
    {
        var dateStr = date.ToString("yyyyMMddTHHmmssZ");
        conditions.Add(new List<(string, string, string)> { ("eq", "$x-amz-date", dateStr) });
        // this.formData.Add("x-amz-date", dateStr);
    }

    /// <summary>
    ///     Set base64 encoded policy to form dictionary.
    /// </summary>
    /// <param name="policyBase64">Base64 encoded policy</param>
    public void SetPolicy(string policyBase64)
    {
        // this.formData.Add("policy", policyBase64);
    }

    /// <summary>
    ///     Set computed signature for the policy to form dictionary.
    /// </summary>
    /// <param name="signature">Computed signature</param>
    public void SetSignature(string signature)
    {
        // this.formData.Add("x-amz-signature", signature);
    }

    /// <summary>
    ///     Serialize policy into JSON string.
    /// </summary>
    /// <returns>Serialized JSON policy</returns>
    private byte[] MarshalJSON()
    {
        var policyList = new List<string>();
        foreach (var condition in conditions)
            policyList.Add("[\"" + condition[0].Item1 + "\",\"" + condition[0].Item2 + "\",\"" +
                           condition[0].Item3 + "\"]");

        // expiration and policies will never be empty because of checks at PresignedPostPolicy()
        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append("\"expiration\":\"").Append(expiration.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")).Append('"')
            .Append(',');
        sb.Append("\"conditions\":[").Append(string.Join(",", policyList)).Append(']');
        sb.Append('}');
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    ///     Compute base64 encoded form of JSON policy.
    /// </summary>
    /// <returns>Base64 encoded string of JSON policy</returns>
    public string Base64()
    {
        var policyStrBytes = MarshalJSON();
        return Convert.ToBase64String(policyStrBytes);
    }

    /// <summary>
    ///     Verify if bucket is set in policy.
    /// </summary>
    /// <returns>true if bucket is set</returns>
    public bool IsBucketSet()
    {
        if (formData.TryGetValue("bucket", out var value))
            if (!string.IsNullOrEmpty(value))
                return true;

        return false;
    }

    /// <summary>
    ///     Verify if key is set in policy.
    /// </summary>
    /// <returns>true if key is set</returns>
    public bool IsKeySet()
    {
        if (formData.TryGetValue("key", out var value))
            if (!string.IsNullOrEmpty(value))
                return true;

        return false;
    }

    /// <summary>
    ///     Verify if expiration is set in policy.
    /// </summary>
    /// <returns>true if expiration is set</returns>
    public bool IsExpirationSet()
    {
        if (!string.IsNullOrEmpty(expiration.ToString())) return true;

        return false;
    }

    /// <summary>
    ///     Get the populated dictionary of policy data.
    /// </summary>
    /// <returns>Dictionary of policy data</returns>
    public Dictionary<string, string> GetFormData()
    {
        return formData;
    }
}