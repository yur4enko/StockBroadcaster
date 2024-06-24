namespace StockBroadcaster.ApiService.Domain.StockBroadcastService
{
    public interface IStockTickerHubClient
    {
        Task StockUpdate(StockUpdateData data);
    }
}
