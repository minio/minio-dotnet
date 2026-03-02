using System.Text.Json;
using System.Text.Json.Serialization;

namespace Minio.Helpers;

/// <summary>
/// A <see cref="JsonConverter{T}"/> that serializes and deserializes <see cref="TimeSpan"/> values
/// as a 64-bit integer number of nanoseconds. This format is used by MinIO and S3 APIs that express
/// durations in nanosecond precision.
/// </summary>
public sealed class NanoSecTimeSpanJsonConverter : JsonConverter<TimeSpan>
{
#if NET7_0_OR_GREATER
    /// <summary>
    /// Reads a JSON integer representing a duration in nanoseconds and converts it to a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="reader">The reader positioned at the JSON number token.</param>
    /// <param name="typeToConvert">The target type; always <see cref="TimeSpan"/>.</param>
    /// <param name="options">The serializer options (unused).</param>
    /// <returns>A <see cref="TimeSpan"/> equivalent to the number of nanoseconds read from JSON.</returns>
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetInt64() / TimeSpan.NanosecondsPerTick);

    /// <summary>
    /// Writes a <see cref="TimeSpan"/> as a JSON integer representing the duration in nanoseconds.
    /// </summary>
    /// <param name="writer">The writer to which the JSON number is written.</param>
    /// <param name="value">The <see cref="TimeSpan"/> value to serialize.</param>
    /// <param name="options">The serializer options (unused).</param>
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteNumberValue((long)value.TotalNanoseconds);
#else
    /// <summary>
    /// Reads a JSON integer representing a duration in nanoseconds and converts it to a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="reader">The reader positioned at the JSON number token.</param>
    /// <param name="typeToConvert">The target type; always <see cref="TimeSpan"/>.</param>
    /// <param name="options">The serializer options (unused).</param>
    /// <returns>A <see cref="TimeSpan"/> equivalent to the number of nanoseconds read from JSON.</returns>
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => TimeSpan.FromMilliseconds(reader.GetInt64() / TimeSpan.TicksPerMillisecond);

    /// <summary>
    /// Writes a <see cref="TimeSpan"/> as a JSON integer representing the duration in nanoseconds.
    /// </summary>
    /// <param name="writer">The writer to which the JSON number is written.</param>
    /// <param name="value">The <see cref="TimeSpan"/> value to serialize.</param>
    /// <param name="options">The serializer options (unused).</param>
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Ticks * 100);
#endif
}