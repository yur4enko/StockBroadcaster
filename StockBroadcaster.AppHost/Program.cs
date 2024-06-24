using Aspire.Hosting;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var cache = builder.AddRedis("cache");
        var apiService = builder.AddProject<Projects.StockBroadcaster_ApiService>("apiservice").
            WithReference(cache);

        builder.Build().Run();
    }
}
