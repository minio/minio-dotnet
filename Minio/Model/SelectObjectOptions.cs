using System.Xml.Linq;

namespace Minio.Model;

/// <summary>The expression type for S3 Select queries.</summary>
public enum SelectExpressionType { Sql }

/// <summary>Controls how header rows are handled in CSV input.</summary>
public enum CsvFileHeaderInfo
{
    /// <summary>Treats the first row as data, not column names.</summary>
    None,
    /// <summary>Uses the first row as column names in the SQL expression.</summary>
    Use,
    /// <summary>Ignores the first row.</summary>
    Ignore,
}

/// <summary>JSON input document type.</summary>
public enum JsonType
{
    /// <summary>Each top-level JSON value is treated as a record.</summary>
    Document,
    /// <summary>Each newline-delimited JSON object is treated as a record.</summary>
    Lines,
}

/// <summary>Controls quoting of output CSV fields.</summary>
public enum CsvQuoteFields
{
    /// <summary>Quote fields only when necessary.</summary>
    AsNeeded,
    /// <summary>Always quote all fields.</summary>
    Always,
}

/// <summary>CSV input serialization configuration.</summary>
public class CsvInput
{
    public CsvFileHeaderInfo FileHeaderInfo { get; set; } = CsvFileHeaderInfo.None;
    public string? RecordDelimiter { get; set; }
    public string? FieldDelimiter { get; set; }
    public string? QuoteCharacter { get; set; }
    public string? QuoteEscapeCharacter { get; set; }
    public string? CommentCharacter { get; set; }

    internal XElement Serialize()
    {
        var xCsv = new XElement("CSV",
            new XElement("FileHeaderInfo", FileHeaderInfo.ToString().ToUpperInvariant()));
        if (RecordDelimiter != null) xCsv.Add(new XElement("RecordDelimiter", RecordDelimiter));
        if (FieldDelimiter != null) xCsv.Add(new XElement("FieldDelimiter", FieldDelimiter));
        if (QuoteCharacter != null) xCsv.Add(new XElement("QuoteCharacter", QuoteCharacter));
        if (QuoteEscapeCharacter != null) xCsv.Add(new XElement("QuoteEscapeCharacter", QuoteEscapeCharacter));
        if (CommentCharacter != null) xCsv.Add(new XElement("Comments", CommentCharacter));
        return new XElement("InputSerialization", xCsv);
    }
}

/// <summary>JSON input serialization configuration.</summary>
public class JsonInput
{
    public JsonType Type { get; set; } = JsonType.Document;

    internal XElement Serialize() =>
        new XElement("InputSerialization",
            new XElement("JSON",
                new XElement("Type", Type.ToString().ToUpperInvariant())));
}

/// <summary>Parquet input serialization configuration (no additional settings required).</summary>
public class ParquetInput
{
    internal XElement Serialize() =>
        new XElement("InputSerialization", new XElement("Parquet"));
}

/// <summary>CSV output serialization configuration.</summary>
public class CsvOutput
{
    public CsvQuoteFields? QuoteFields { get; set; }
    public string? RecordDelimiter { get; set; }
    public string? FieldDelimiter { get; set; }
    public string? QuoteCharacter { get; set; }
    public string? QuoteEscapeCharacter { get; set; }

    internal XElement Serialize()
    {
        var xCsv = new XElement("CSV");
        if (QuoteFields.HasValue)
            xCsv.Add(new XElement("QuoteFields", QuoteFields.Value == CsvQuoteFields.Always ? "ALWAYS" : "ASNEEDED"));
        if (RecordDelimiter != null) xCsv.Add(new XElement("RecordDelimiter", RecordDelimiter));
        if (FieldDelimiter != null) xCsv.Add(new XElement("FieldDelimiter", FieldDelimiter));
        if (QuoteCharacter != null) xCsv.Add(new XElement("QuoteCharacter", QuoteCharacter));
        if (QuoteEscapeCharacter != null) xCsv.Add(new XElement("QuoteEscapeCharacter", QuoteEscapeCharacter));
        return new XElement("OutputSerialization", xCsv);
    }
}

/// <summary>JSON output serialization configuration.</summary>
public class JsonOutput
{
    public string? RecordDelimiter { get; set; }

    internal XElement Serialize()
    {
        var xJson = new XElement("JSON");
        if (RecordDelimiter != null) xJson.Add(new XElement("RecordDelimiter", RecordDelimiter));
        return new XElement("OutputSerialization", xJson);
    }
}

/// <summary>Defines the byte range to scan within the object for S3 Select.</summary>
public record SelectScanRange(long Start, long End);

/// <summary>Options for an S3 Select query.</summary>
public class SelectObjectOptions
{
    /// <summary>The SQL expression to execute against the object.</summary>
    public required string Expression { get; set; }

    /// <summary>The expression type. Currently only <see cref="SelectExpressionType.Sql"/> is supported.</summary>
    public SelectExpressionType ExpressionType { get; set; } = SelectExpressionType.Sql;

    /// <summary>
    /// Input serialization format. Must be one of <see cref="CsvInput"/>, <see cref="JsonInput"/>,
    /// or <see cref="ParquetInput"/>.
    /// </summary>
    public required object InputSerialization { get; set; }

    /// <summary>
    /// Output serialization format. Must be one of <see cref="CsvOutput"/> or <see cref="JsonOutput"/>.
    /// </summary>
    public required object OutputSerialization { get; set; }

    /// <summary>When <see langword="true"/>, periodically returns progress statistics during the query.</summary>
    public bool? RequestProgress { get; set; }

    /// <summary>Limits the bytes scanned to a sub-range of the object.</summary>
    public SelectScanRange? ScanRange { get; set; }

    /// <summary>Serializes these options to the S3 SelectObjectContent request XML body.</summary>
    public XElement Serialize()
    {
        var xReq = new XElement("SelectObjectContentRequest",
            new XElement("Expression", Expression),
            new XElement("ExpressionType", ExpressionType.ToString().ToUpperInvariant()));

        xReq.Add(InputSerialization switch
        {
            CsvInput csv => csv.Serialize(),
            JsonInput json => json.Serialize(),
            ParquetInput parquet => parquet.Serialize(),
            _ => throw new ArgumentException(
                "InputSerialization must be CsvInput, JsonInput, or ParquetInput",
                nameof(InputSerialization))
        });

        xReq.Add(OutputSerialization switch
        {
            CsvOutput csv => csv.Serialize(),
            JsonOutput json => json.Serialize(),
            _ => throw new ArgumentException(
                "OutputSerialization must be CsvOutput or JsonOutput",
                nameof(OutputSerialization))
        });

        if (RequestProgress.HasValue)
            xReq.Add(new XElement("RequestProgress",
                new XElement("Enabled", RequestProgress.Value ? "true" : "false")));

        if (ScanRange != null)
            xReq.Add(new XElement("ScanRange",
                new XElement("Start", ScanRange.Start),
                new XElement("End", ScanRange.End)));

        return xReq;
    }
}
