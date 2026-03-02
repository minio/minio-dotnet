using System.Text;

namespace Minio.Helpers;

internal static class ByteExtensions
{
    public static string ToHexStringLowercase(this byte[] data)
    {
        char ToHex(byte value) => (char)(value < 10 ? '0' + value : 'a' + value - 10);

        var sb = new StringBuilder(data.Length * 2);
        foreach (var b in data)
        {
            sb.Append(ToHex((byte)(b >> 4)));
            sb.Append(ToHex((byte)(b & 0xf)));
        }
        return sb.ToString();

    }
}