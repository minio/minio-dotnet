using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Minio.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<BuilderBenchmarks>();
    }
}

[MemoryDiagnoser]
public class BuilderBenchmarks
{
    [Benchmark]
    public MinioClientBuilder BuildClient()
    {
        return new MinioClientBuilder("https://play.min.io")
            .WithStaticCredentials("accessKey", "secretKey");
    }

    [Benchmark]
    public MinioClientBuilder BuildClientWithRegion()
    {
        return new MinioClientBuilder("https://play.min.io")
            .WithStaticCredentials("accessKey", "secretKey")
            .WithRegion("us-east-1");
    }
}
