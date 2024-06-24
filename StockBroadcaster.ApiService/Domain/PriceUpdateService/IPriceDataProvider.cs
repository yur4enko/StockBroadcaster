namespace StockBroadcaster.ApiService.Domain.PriceUpdateService
{
    public interface IPriceDataProvider
    {
        Task<PriceUpdate> GetPriceUpdateAsync(string currencyExchangeSymbol, CancellationToken ct = default);
    }
}
