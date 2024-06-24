using Microsoft.AspNetCore.SignalR;
using StockBroadcaster.ApiService.Domain.PriceUpdateService;
using StockBroadcaster.ApiService.Domain.StockBroadcastService;
using StockBroadcaster.ApiService.Infrastructure.Hubs;
using System.Collections.Concurrent;

namespace StockBroadcaster.ApiService.Application.Services
{
    /// <summary>
    /// Leveraging the .NET Aspire orchestrator, the StockBroadcaster application is designed for seamless scalability, 
    /// especially in cloud environments. By abstracting away complex infrastructure concerns, Aspire enables 
    /// effortless scaling of resources to accommodate any workload, ensuring optimal performance and responsiveness 
    /// even under high demand.
    /// </summary>

    public class StockBroadcastService : IStockBroadcastService
    {
        private readonly IPriceUpdateService _priceUpdateService;
        private readonly IHubContext<StockTickerHub, IStockTickerHubClient> _stockTickerHubContext;
        private readonly ILogger<StockBroadcastService> _logger;

        private readonly ConcurrentDictionary<string, string> _idToCurrency = new();
        private readonly ConcurrentDictionary<string, int> _onlineSubscribers = new();
        private readonly object _subscriptionLock = new object();
        private readonly IEnumerable<string> _availableCurrencyExchangeSymbols;

        public StockBroadcastService(
            IPriceUpdateService priceUpdateService, 
            IHubContext<StockTickerHub, IStockTickerHubClient> stockTickerHubContext, 
            ILogger<StockBroadcastService> logger)
        {
            _priceUpdateService = priceUpdateService;
            _stockTickerHubContext = stockTickerHubContext;
            _logger = logger;

            _availableCurrencyExchangeSymbols = _priceUpdateService.GetAvailableCurrencyExchangeSymbols();
            _logger.LogInformation("StockBroadcastService created");
        }

        async Task IStockBroadcastService.SubscribeAsync(string connectionId, string currencyExchangeSymbol, CancellationToken cancellationToken)
        {
            _logger.LogTrace($"Subscribing new connection to {currencyExchangeSymbol} group.");

            if (!_availableCurrencyExchangeSymbols.Contains(currencyExchangeSymbol))
            {
                _logger.LogWarning($"Attempt to subscribe for unsupported currencyExchangeSymbol '{currencyExchangeSymbol}'");
                throw new FinancialInstrumentNotFoundException(currencyExchangeSymbol);
            }

            lock (_subscriptionLock)
            {
                _idToCurrency[connectionId] = currencyExchangeSymbol;
                _onlineSubscribers[currencyExchangeSymbol] = _onlineSubscribers.GetValueOrDefault(currencyExchangeSymbol, 0) + 1;

                if (_onlineSubscribers[currencyExchangeSymbol] == 1)
                {
                    // Start price updates if this is the first subscriber for the symbol.
                    // This ensures we only fetch data from the provider once for each symbol,
                    // regardless of the number of subscribers.
                    Task.Run(() => _priceUpdateService.StartExchangeSymbolPriceWatchAsync(currencyExchangeSymbol,
                        priceUpdate => SendUpdatedPriceToGroupAsync(currencyExchangeSymbol, priceUpdate)), cancellationToken);
                }
            }

            _ = Task.Run(() => SendInitialPriceToCaller(connectionId, currencyExchangeSymbol));
        }

        private async Task SendInitialPriceToCaller(string connectionId, string currencyExchangeSymbol)
        {
            var priceData = await _priceUpdateService.GetCurrentPriceUpdateAsync(currencyExchangeSymbol);
            await _stockTickerHubContext.Clients.Client(connectionId).StockUpdate(new StockUpdateData(priceData.Price));
        }

        async Task SendUpdatedPriceToGroupAsync(string currencyExchangeSymbol, PriceUpdate priceUpdate)
        {
            _logger.LogTrace($"Broadcast updated price for {currencyExchangeSymbol}.");
            // SignalR handles efficient message delivery to all clients in the group,
            // even with 1,000+ subscribers. This avoids the need to loop through each
            // subscriber and send individual messages, which would be much less efficient.
            await _stockTickerHubContext.Clients.Group(currencyExchangeSymbol).StockUpdate(new StockUpdateData(priceUpdate.Price));
        }

        async Task<string> IStockBroadcastService.UnsubscribeAsync(string connectionId, CancellationToken cancellationToken)
        {
            string currencyExchangeSymbol = null;
            lock (_subscriptionLock)
            {
                _idToCurrency.TryRemove(connectionId, out currencyExchangeSymbol);
                if (currencyExchangeSymbol == null)
                {
                    return currencyExchangeSymbol;
                }

                _logger.LogTrace($"Remove subscription from {currencyExchangeSymbol} group.");

                var subscribers = _onlineSubscribers.GetValueOrDefault(currencyExchangeSymbol, 0) - 1;
                _onlineSubscribers[currencyExchangeSymbol] = subscribers > 0 ? subscribers : 0;

                if (subscribers == 0)
                {
                    _logger.LogInformation($"No more clients for {currencyExchangeSymbol} left. Stopping exchange watch.");
                    // Stop price updates if there are no more subscribers for the symbol.
                    // This helps conserve resources by not fetching data that's no longer needed.
                    _priceUpdateService.StopExchangeSymbolPriceWatchAsync(currencyExchangeSymbol);
                }
            }

            return currencyExchangeSymbol;
        }
    }
}
