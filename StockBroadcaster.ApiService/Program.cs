using Microsoft.AspNetCore.Http.Connections;
using StockBroadcaster.ApiService.Application;
using StockBroadcaster.ApiService.Application.Services;
using StockBroadcaster.ApiService.Domain.PriceUpdateService;
using StockBroadcaster.ApiService.Domain.StockBroadcastService;
using StockBroadcaster.ApiService.Infrastructure.Hubs;
using StockBroadcaster.ApiService.Infrastructure.Options;

internal class Program
{
    /// <summary>
    /// Leveraging the .NET Aspire orchestrator, the StockBroadcaster application is designed for seamless scalability, 
    /// especially in cloud environments. By abstracting away complex infrastructure concerns, Aspire enables 
    /// effortless scaling of resources to accommodate any workload, ensuring optimal performance and responsiveness 
    /// even under high demand.
    /// </summary>
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire components.
        builder.AddServiceDefaults();
        builder.AddRedisDistributedCache("cache");

        var procDir = Path.GetDirectoryName(Environment.ProcessPath);
        builder.Logging.AddFile(Path.Join(procDir, "Logs/StockBroadcaster.ApiService-{Date}.txt"));

        // Add services to the container.

        builder.Services.AddProblemDetails();

        builder.Services.Configure<AlphaVantageOptions>(builder.Configuration.GetSection("AlphaVantage"));
        builder.Services.AddHttpClient<IPriceDataProvider, AlphaVantageClient>();
        builder.Services.AddSingleton<IPriceUpdateService, PriceUpdateService>();
        builder.Services.AddSingleton<IStockBroadcastService, StockBroadcastService>();
        builder.Services.AddControllers();

        builder.Services.AddSignalR(hubOptions =>
        {
            hubOptions.EnableDetailedErrors = true;
        }).AddJsonProtocol().AddHubOptions<StockTickerHub>(options =>
        {
            options.EnableDetailedErrors = true;
        }); ;

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();
        app.MapDefaultEndpoints();

        // register SingleR endpoint
        app.MapHub<StockTickerHub>("/stockticker", options =>
        {
            options.Transports = HttpTransportType.WebSockets;
        });


        app.Run();
    }
}
