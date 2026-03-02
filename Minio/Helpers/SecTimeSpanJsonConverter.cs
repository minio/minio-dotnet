using System.Text.Json;
using System.Text.Json.Serialization;

namespace Minio.Helpers;

/// <summary>
/// A <see cref="JsonConverter{T}"/> that serializes and deserializes <see cref="TimeSpan"/> values
/// as a 64-bit integer number of whole seconds. This format is used by MinIO and S3 APIs that express
/// durations in second-level precision (for example, token expiry lifetimes).
/// </summary>
public sealed class SecTimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    /// <summary>
    /// Reads a JSON integer representing a duration in seconds and converts it to a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="reader">The reader positioned at the JSON number token.</param>
    /// <param name="typeToConvert">The target type; always <see cref="TimeSpan"/>.</param>
    /// <param name="options">The serializer options (unused).</param>
    /// <returns>A <see cref="TimeSpan"/> equivalent to the number of seconds read from JSON.</returns>
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => TimeSpan.FromSeconds(reader.GetInt64());

    /// <summary>
    /// Writes a <see cref="TimeSpan"/> as a JSON integer representing the duration in whole seconds.
    /// </summary>
    /// <param name="writer">The writer to which the JSON number is written.</param>
    /// <param name="value">The <see cref="TimeSpan"/> value to serialize.</param>
    /// <param name="options">The serializer options (unused).</param>
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteNumberValue((long)value.TotalSeconds);
}