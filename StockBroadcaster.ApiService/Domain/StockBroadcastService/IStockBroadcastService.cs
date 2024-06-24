namespace StockBroadcaster.ApiService.Domain.StockBroadcastService
{
    public interface IStockBroadcastService
    {
        public Task SubscribeAsync(string connectionId, string currencyExchangeSymbol, CancellationToken cancellationToken = default);
        public Task<string> UnsubscribeAsync(string connectionId, CancellationToken cancellationToken = default);
    }
}
