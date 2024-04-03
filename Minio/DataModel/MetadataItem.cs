namespace Minio.DataModel;

[Serializable]
public class MetadataItem
{
    public MetadataItem()
    {
    }

    public MetadataItem(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; set; }
    public string Value { get; set; }
}
