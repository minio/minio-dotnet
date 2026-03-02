#if NET6_0

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable 

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class RequiredMemberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        // ReSharper disable once UnusedParameter.Local
        public CompilerFeatureRequiredAttribute(string _) { }
    }

    [AttributeUsage(AttributeTargets.Constructor)]
    internal sealed class SetsRequiredMembersAttribute : Attribute
    {
    }    
}

namespace Shims
{
internal sealed class GeneratedRegexAttribute : Attribute
{
    // ReSharper disable once UnusedParameter.Local
    public GeneratedRegexAttribute(string _) { }
}

internal static class ArgumentException
{
    public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (string.IsNullOrEmpty(argument))
        {
            ArgumentNullException.ThrowIfNull(argument, paramName);
            throw new System.ArgumentException("Argument cannot be an empty string", paramName);
        }
    }  
}

internal static class ArgumentNullException
{
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument == null)
            throw new System.ArgumentNullException(paramName, "Argument cannot be null");
    }  
}

// ReSharper disable once InconsistentNaming
internal static class SHA256
{
    public static byte[] HashData(byte[] data)
    {
        using var hasher = System.Security.Cryptography.SHA256.Create();
        return hasher.ComputeHash(data);
    }

    public static byte[] HashData(Stream stream)
    {
        using var hasher = System.Security.Cryptography.SHA256.Create();
        return hasher.ComputeHash(stream);
    }

    public static async Task<byte[]> HashDataAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var hasher = System.Security.Cryptography.SHA256.Create();
        return await hasher.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
    }
}

// ReSharper disable once InconsistentNaming
internal static class MD5
{
    public static byte[] HashData(byte[] data)
    {
        using var hasher = System.Security.Cryptography.MD5.Create();
        return hasher.ComputeHash(data);
    }

    public static byte[] HashData(Stream stream)
    {
        using var hasher = System.Security.Cryptography.MD5.Create();
        return hasher.ComputeHash(stream);
    }

    public static async Task<byte[]> HashDataAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var hasher = System.Security.Cryptography.MD5.Create();
        return await hasher.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
    }
}
}

#endif