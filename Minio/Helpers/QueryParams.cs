using System.Text;

namespace Minio.Helpers;

internal class QueryParams
{
    private Dictionary<string, List<string>>? _params;

    public QueryParams Add(string name, string value)
    {
        _params ??= new();
        if (!_params.TryGetValue(name, out var values))
        {
            values = new List<string>(1);
            _params.Add(name, values);
        }
        values.Add(value);
        return this;
    }

    public QueryParams AddIfNotNull(string name, string? value)
    {
        if (value != null)
            Add(name, value);
        return this;
    }

    public QueryParams AddIfNotNullOrEmpty(string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            Add(name, value);
        return this;
    }

    public IReadOnlyList<string> Get(string name)
    {
        if (_params != null && _params.TryGetValue(name, out var values))
            return values;
        return Array.Empty<string>();
    }

    public override string ToString()
    {
        if (_params == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var (name, values) in _params)
        {
            var encodedName = Uri.EscapeDataString(name);
            foreach (var value in values)
            {
                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.Append(encodedName);
                sb.Append('=');
                if (!string.IsNullOrEmpty(value))
                    sb.Append(Uri.EscapeDataString(value));
            }
        }

        return sb.ToString();
    }
}