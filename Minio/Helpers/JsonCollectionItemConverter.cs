using System.Text.Json;
using System.Text.Json.Serialization;

namespace Minio.Helpers;

/// <summary>
/// A <see cref="JsonConverter{T}"/> for <see cref="List{T}"/> that applies a per-element converter
/// of type <typeparamref name="TConverterType"/> when serializing and deserializing each item in the collection.
/// This is useful when the element type requires a custom JSON representation that differs from the
/// default serializer behaviour.
/// </summary>
/// <typeparam name="TDatatype">The type of the elements stored in the list.</typeparam>
/// <typeparam name="TConverterType">
/// The <see cref="JsonConverter"/> type to instantiate and apply to each individual element
/// during read and write operations. Must have a public parameterless constructor.
/// </typeparam>
public class JsonCollectionItemConverter<TDatatype, TConverterType> : JsonConverter<List<TDatatype>>
    where TConverterType : JsonConverter
{
    /// <summary>
    /// Reads and converts JSON into a <see cref="List{T}"/> of <typeparamref name="TDatatype"/>,
    /// using a fresh instance of <typeparamref name="TConverterType"/> for each element.
    /// Returns <see langword="null"/> when the JSON token is <c>null</c>.
    /// </summary>
    /// <param name="reader">The reader positioned at the start of the JSON value to deserialize.</param>
    /// <param name="typeToConvert">The target type; always <see cref="List{T}"/> of <typeparamref name="TDatatype"/>.</param>
    /// <param name="options">The serializer options to use as a base; element-level converters are replaced with <typeparamref name="TConverterType"/>.</param>
    /// <returns>A <see cref="List{T}"/> containing the deserialized elements, or <see langword="null"/> if the JSON token is null.</returns>
    public override List<TDatatype> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        var jsonSerializerOptions = new JsonSerializerOptions(options);
        jsonSerializerOptions.Converters.Clear();
        jsonSerializerOptions.Converters.Add(Activator.CreateInstance<TConverterType>());

        var returnValue = new List<TDatatype>();

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                returnValue.Add((TDatatype)JsonSerializer.Deserialize(ref reader, typeof(TDatatype), jsonSerializerOptions));
            reader.Read();
        }

        return returnValue;
    }

    /// <summary>
    /// Writes a <see cref="List{T}"/> of <typeparamref name="TDatatype"/> as a JSON array,
    /// using a fresh instance of <typeparamref name="TConverterType"/> for each element.
    /// Writes a JSON <c>null</c> when <paramref name="value"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="writer">The writer to which the JSON array is written.</param>
    /// <param name="value">The list to serialize, or <see langword="null"/> to write a JSON null value.</param>
    /// <param name="options">The serializer options to use as a base; element-level converters are replaced with <typeparamref name="TConverterType"/>.</param>
    public override void Write(Utf8JsonWriter writer, List<TDatatype>? value, JsonSerializerOptions options)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
            
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var jsonSerializerOptions = new JsonSerializerOptions(options);
        jsonSerializerOptions.Converters.Clear();
        jsonSerializerOptions.Converters.Add(Activator.CreateInstance<TConverterType>());

        writer.WriteStartArray();
        foreach (TDatatype data in value)
            JsonSerializer.Serialize(writer, data, jsonSerializerOptions);
        writer.WriteEndArray();
    }
}