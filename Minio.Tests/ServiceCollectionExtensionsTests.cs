using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Minio.Tests;

[TestClass]
public class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void RegistersService()
    {
        var services = new ServiceCollection();
        var accessKey = Guid.NewGuid().ToString();
        var secretKey = Guid.NewGuid().ToString();

        _ = services.AddMinio(accessKey, secretKey);
        using var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetService<IMinioClient>();

        Assert.IsNotNull(client);
        Assert.IsInstanceOfType(client, typeof(MinioClient));
        Assert.AreEqual(client.Config.AccessKey, accessKey);
        Assert.AreEqual(client.Config.SecretKey, secretKey);
    }

    [TestMethod]
    public void RegistersKeyedService()
    {
        var services = new ServiceCollection();
        var serviceKey = new object();
        var accessKey = Guid.NewGuid().ToString();
        var secretKey = Guid.NewGuid().ToString();

        _ = services.AddKeyedMinio(accessKey, secretKey, serviceKey);
        using var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetKeyedService<IMinioClient>(serviceKey);

        Assert.IsNotNull(client);
        Assert.IsInstanceOfType(client, typeof(MinioClient));
        Assert.AreEqual(client.Config.AccessKey, accessKey);
        Assert.AreEqual(client.Config.SecretKey, secretKey);
    }

    [TestMethod]
    public void RegistersKeyedServiceWithNullKey()
    {
        var services = new ServiceCollection();
        object serviceKey = null;
        var accessKey = Guid.NewGuid().ToString();
        var secretKey = Guid.NewGuid().ToString();

        _ = services.AddKeyedMinio(accessKey, secretKey, serviceKey);
        using var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetKeyedService<IMinioClient>(serviceKey);
        var client2 = serviceProvider.GetService<IMinioClient>();

        Assert.IsNotNull(client);
        Assert.IsNotNull(client2);
        Assert.AreEqual(client, client2);
        Assert.IsInstanceOfType(client, typeof(MinioClient));
        Assert.AreEqual(client.Config.AccessKey, accessKey);
        Assert.AreEqual(client.Config.SecretKey, secretKey);
    }

    [TestMethod]
    public void RegisterServiceFailsWhenPassedNullServiceCollection()
    {
        IServiceCollection services = null;
        static void configureClient(IMinioClient client) { }

        _ = Assert.ThrowsException<ArgumentNullException>(() => services.AddMinio(configureClient));
    }

    [TestMethod]
    public void RegisterServiceFailsWhenPassedNullConfigureClient()
    {
        IServiceCollection services = new ServiceCollection();
        Action<IMinioClient> configureClient = null;

        _ = Assert.ThrowsException<ArgumentNullException>(() => services.AddMinio(configureClient));
    }

    [TestMethod]
    public void RegisterKeyedServiceFailsWhenPassedNullServiceCollection()
    {
        IServiceCollection services = null;
        static void configureClient(IMinioClient client) { }
        object key = new();

        _ = Assert.ThrowsException<ArgumentNullException>(() => services.AddKeyedMinio(configureClient, key));
    }

    [TestMethod]
    public void RegisterKeyedServiceFailsWhenPassedNullConfigureClient()
    {
        IServiceCollection services = new ServiceCollection();
        Action<IMinioClient> configureClient = null;
        object key = new();

        _ = Assert.ThrowsException<ArgumentNullException>(() => services.AddKeyedMinio(configureClient, key));
    }
}
