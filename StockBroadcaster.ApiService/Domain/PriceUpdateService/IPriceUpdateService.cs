namespace StockBroadcaster.ApiService.Domain.PriceUpdateService
{
    public interface IPriceUpdateService
    {
        IEnumerable<string> GetAvailableCurrencyExchangeSymbols();
        Task<PriceUpdate> GetCurrentPriceUpdateAsync(string currencyExchangeSymbol, CancellationToken cancellationToken = default);
        Task StartExchangeSymbolPriceWatchAsync(string currencyExchangeSymbol, Func<PriceUpdate, Task> onPriceUpdate);
        Task StopExchangeSymbolPriceWatchAsync(string currencyExchangeSymbol);
    }
}
