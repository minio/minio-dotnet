using System.Text.Json;

namespace Minio.Model;

/// <summary>
/// A condition in a presigned POST policy, constraining what values are acceptable
/// in a browser-based upload request.
/// </summary>
public abstract class PostPolicyCondition
{
    /// <summary>Creates an exact-match condition for the specified field.</summary>
    public static PostPolicyCondition Exact(string field, string value) => new ExactMatchCondition(field, value);

    /// <summary>Creates a starts-with condition for the specified field.</summary>
    public static PostPolicyCondition StartsWith(string field, string prefix) => new StartsWithCondition(field, prefix);

    /// <summary>Creates a content-length-range condition.</summary>
    public static PostPolicyCondition ContentLengthRange(long min, long max) => new ContentLengthRangeCondition(min, max);

    internal abstract string ToJson();

    private sealed class ExactMatchCondition(string field, string value) : PostPolicyCondition
    {
        internal override string ToJson()
        {
            var fieldJson = JsonSerializer.Serialize(field);
            var valueJson = JsonSerializer.Serialize(value);
            return $"{{{fieldJson}:{valueJson}}}";
        }
    }

    private sealed class StartsWithCondition(string field, string prefix) : PostPolicyCondition
    {
        internal override string ToJson()
        {
            var fieldJson = JsonSerializer.Serialize("$" + field);
            var prefixJson = JsonSerializer.Serialize(prefix);
            return $"[\"starts-with\",{fieldJson},{prefixJson}]";
        }
    }

    private sealed class ContentLengthRangeCondition(long min, long max) : PostPolicyCondition
    {
        internal override string ToJson() => $"[\"content-length-range\",{min},{max}]";
    }
}
