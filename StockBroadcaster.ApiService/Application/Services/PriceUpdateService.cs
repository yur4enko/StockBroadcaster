using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StockBroadcaster.ApiService.Domain.FinancialInstruments;
using StockBroadcaster.ApiService.Domain.PriceUpdateService;

namespace StockBroadcaster.ApiService.Application.Services
{
    /// <summary>
    /// Leveraging the .NET Aspire orchestrator, the StockBroadcaster application is designed for seamless scalability, 
    /// especially in cloud environments. By abstracting away complex infrastructure concerns, Aspire enables 
    /// effortless scaling of resources to accommodate any workload, ensuring optimal performance and responsiveness 
    /// even under high demand.
    /// </summary>
    public class PriceUpdateService : IPriceUpdateService
    {
        private readonly IPriceDataProvider _priceDataProvider;
        private readonly IDistributedCache _cache;
        private readonly ILogger<PriceUpdateService> _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _symbolLocks = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _currencyWatchers = new();
        private readonly IEnumerable<string> _availableCurrencyExchangeSymbols;

        public PriceUpdateService(
            IPriceDataProvider priceDataProvider,
            IDistributedCache cache,
            ILogger<PriceUpdateService> logger)
        {
            _priceDataProvider = priceDataProvider;
            _cache = cache;
            _logger = logger;

            var structType = typeof(CurrencyExchangeSymbols);
            _availableCurrencyExchangeSymbols = structType.GetProperties().Select(item => item.GetValue(item).ToString()).ToList();
        }

        public IEnumerable<string> GetAvailableCurrencyExchangeSymbols()
        {
            return _availableCurrencyExchangeSymbols;
        }

        public async Task<PriceUpdate> GetCurrentPriceUpdateAsync(string currencyExchangeSymbol, CancellationToken cancellationToken = default)
        {
            if (!_availableCurrencyExchangeSymbols.Contains(currencyExchangeSymbol))
            {
                _logger.LogWarning($"Attempt to get price update for unsupported currencyExchangeSymbol '{currencyExchangeSymbol}'");
                throw new FinancialInstrumentNotFoundException(currencyExchangeSymbol);
            }

            var semaphore = _symbolLocks.GetOrAdd(currencyExchangeSymbol, _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var cachedPriceUpdate = await _cache.GetStringAsync(currencyExchangeSymbol, cancellationToken);
                if (!string.IsNullOrEmpty(cachedPriceUpdate))
                {
                    return JsonSerializer.Deserialize<PriceUpdate>(cachedPriceUpdate);
                }

                var priceUpdate = await _priceDataProvider.GetPriceUpdateAsync(currencyExchangeSymbol, cancellationToken);
                await _cache.SetStringAsync(currencyExchangeSymbol, JsonSerializer.Serialize(priceUpdate), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                }, cancellationToken);

                return priceUpdate;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task StartExchangeSymbolPriceWatchAsync(string currencyExchangeSymbol, Func<PriceUpdate, Task> onPriceUpdate)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            if (!_currencyWatchers.TryAdd(currencyExchangeSymbol, cancellationTokenSource))
            {
                _logger.LogWarning($"Price watch for {currencyExchangeSymbol} already running");
                return;
            }

            var cancellationToken = cancellationTokenSource.Token;

            _logger.LogInformation($"Starting price watch for {currencyExchangeSymbol}");
            PriceUpdate lastPriceUpdate = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                PriceUpdate priceUpdate = null;
                try
                {
                    priceUpdate = await GetCurrentPriceUpdateAsync(currencyExchangeSymbol, cancellationToken);
                }
                catch (Exception ex) when (ex is PriceDataProviderUnavailableException ||
                                            ex is PriceDataProviderException)
                {
                    _logger.LogError(ex, $"Error fetching price update for {currencyExchangeSymbol}");
                }

                Task onPriceUpdateRoutine = Task.CompletedTask;
                if (priceUpdate != null && lastPriceUpdate != null && lastPriceUpdate.Price != priceUpdate.Price)
                {
                    lastPriceUpdate = priceUpdate;
                    onPriceUpdateRoutine = onPriceUpdate(priceUpdate);
                }

                Task.WhenAll(onPriceUpdateRoutine, Task.Delay(TimeSpan.FromMinutes(1))).Wait(cancellationToken);
            }

            _logger.LogInformation($"Price watch stopped for {currencyExchangeSymbol}");
        }

        public async Task StopExchangeSymbolPriceWatchAsync(string currencyExchangeSymbol)
        {
            if (_currencyWatchers.TryRemove(currencyExchangeSymbol, out var cancellationTokenSource))
            {
                _logger.LogDebug($"Stopping price watch for {currencyExchangeSymbol}");
                await cancellationTokenSource.CancelAsync();
                _logger.LogInformation($"Stopped price watch for {currencyExchangeSymbol}");
            }
            else
            {
                _logger.LogWarning($"Price watch for {currencyExchangeSymbol} not running");
            }

            return;
        }
    }
}
