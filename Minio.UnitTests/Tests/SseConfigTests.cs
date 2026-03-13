using System.Net.Http.Headers;
using Minio.Model;
using Xunit;

namespace Minio.UnitTests.Tests;

public class SseConfigTests
{
    [Fact]
    public void SseS3ConfigAddsAES256Header()
    {
        var request = new HttpRequestMessage();
        var config = new SseS3Config();
        
        config.WriteHeaders(request.Headers);
        
        Assert.True(request.Headers.Contains("X-Amz-Server-Side-Encryption"));
        Assert.Equal("AES256", request.Headers.GetValues("X-Amz-Server-Side-Encryption").First());
        Assert.False(request.Headers.Contains("X-Amz-Server-Side-Encryption-Aws-Kms-Key-Id"));
    }

    [Fact]
    public void SseKmsConfigAddsAwsKmsHeader()
    {
        var request = new HttpRequestMessage();
        var config = new SseKmsConfig();
        
        config.WriteHeaders(request.Headers);
        
        Assert.True(request.Headers.Contains("X-Amz-Server-Side-Encryption"));
        Assert.Equal("aws:kms", request.Headers.GetValues("X-Amz-Server-Side-Encryption").First());
    }

    [Fact]
    public void SseKmsConfigWithKeyIdAddsKeyIdHeader()
    {
        var request = new HttpRequestMessage();
        var config = new SseKmsConfig("my-key-id");
        
        config.WriteHeaders(request.Headers);
        
        Assert.True(request.Headers.Contains("X-Amz-Server-Side-Encryption"));
        Assert.Equal("aws:kms", request.Headers.GetValues("X-Amz-Server-Side-Encryption").First());
        Assert.True(request.Headers.Contains("X-Amz-Server-Side-Encryption-Aws-Kms-Key-Id"));
        Assert.Equal("my-key-id", request.Headers.GetValues("X-Amz-Server-Side-Encryption-Aws-Kms-Key-Id").First());
    }

    [Fact]
    public void SseKmsConfigNullKeyIdDoesNotAddKeyIdHeader()
    {
        var request = new HttpRequestMessage();
        var config = new SseKmsConfig(null);
        
        config.WriteHeaders(request.Headers);
        
        Assert.True(request.Headers.Contains("X-Amz-Server-Side-Encryption"));
        Assert.Equal("aws:kms", request.Headers.GetValues("X-Amz-Server-Side-Encryption").First());
        Assert.False(request.Headers.Contains("X-Amz-Server-Side-Encryption-Aws-Kms-Key-Id"));
    }

    [Fact]
    public void SseKmsConfigEmptyKeyIdDoesNotAddKeyIdHeader()
    {
        var request = new HttpRequestMessage();
        var config = new SseKmsConfig("");
        
        config.WriteHeaders(request.Headers);
        
        Assert.True(request.Headers.Contains("X-Amz-Server-Side-Encryption"));
        Assert.Equal("aws:kms", request.Headers.GetValues("X-Amz-Server-Side-Encryption").First());
        Assert.False(request.Headers.Contains("X-Amz-Server-Side-Encryption-Aws-Kms-Key-Id"));
    }

    [Fact]
    public void SseS3ConfigThrowsArgumentNullExceptionForNullHeaders()
    {
        var config = new SseS3Config();
        
        Assert.Throws<ArgumentNullException>(() => config.WriteHeaders(null!));
    }

    [Fact]
    public void SseKmsConfigThrowsArgumentNullExceptionForNullHeaders()
    {
        var config = new SseKmsConfig();
        
        Assert.Throws<ArgumentNullException>(() => config.WriteHeaders(null!));
    }
}
