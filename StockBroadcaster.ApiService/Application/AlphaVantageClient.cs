using Microsoft.Extensions.Options;
using StockBroadcaster.ApiService.Domain;
using System.Text.Json.Serialization;
using StockBroadcaster.ApiService.Infrastructure.Options;
using StockBroadcaster.ApiService.Domain.FinancialInstruments;
using System.Text.Json;
using System.Net;
using System.Globalization;
using StockBroadcaster.ApiService.Domain.PriceUpdateService;

namespace StockBroadcaster.ApiService.Application
{
    public class AlphaVantageClient : IPriceDataProvider
    {
        private const string address = "https://www.alphavantage.co";
        private readonly HttpClient _httpClient;
        private readonly AlphaVantageOptions _options;
        private readonly ILogger<AlphaVantageClient> _logger;

        public AlphaVantageClient(HttpClient httpClient, IOptions<AlphaVantageOptions> options, ILogger<AlphaVantageClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;

            _logger = logger;

            _logger.LogInformation($"AlphaVantageClient for '{address}' created");
        }

        public async Task<PriceUpdate> GetPriceUpdateAsync(string currencyExchangeSymbol, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("GetPriceUpdateAsync initiated for CURRENCY_EXCHANGE_RATE symbol: {Symbol}", currencyExchangeSymbol);

            var (fromCurrency, toCurrency) = CurrencyExchangeSymbols.Split(currencyExchangeSymbol);
            var url = $"{address}/query?function=CURRENCY_EXCHANGE_RATE" +
                      $"&from_currency={fromCurrency}" +
                      $"&to_currency={toCurrency}" +
                      $"&apikey={_options.ApiKey}";


            var responseMessage = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogTrace($"Raw response from Alpha Vantage API: {responseContent}");

            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Price data provider responce status code is not Success: {responseMessage.ToString()}");

                if (responseMessage.StatusCode == HttpStatusCode.InternalServerError || // 500 Internal Server Error
                    responseMessage.StatusCode == HttpStatusCode.ServiceUnavailable) // 503 Service Unavailable
                {
                    _logger.LogError($"Price data provider is unavailable. Status code: {responseMessage.StatusCode}");
                    throw new PriceDataProviderUnavailableException($"Price data provider returned status code: {responseMessage.StatusCode}");
                }
                else
                {
                    _logger.LogError($"Error fetching data from Alpha Vantage API. Status code: {responseMessage.StatusCode}, Content: {responseContent}");
                    throw new PriceDataProviderException($"Error fetching data from Alpha Vantage API. Status code: {responseMessage.StatusCode}");
                }
            }

            try
            {
                var response = JsonSerializer.Deserialize<AlphaVantageResponse>(responseContent);

                if (response == null || response.RealtimeCurrencyExchangeRate == null)
                {
                    _logger.LogError($"Invalid response from Alpha Vantage API for {currencyExchangeSymbol}. " +
                        $"Content: '{responseContent}'. Parsed: '{response?.ToString()}'");
                    throw new PriceDataProviderException("Invalid response from Alpha Vantage API.");
                }

                var price = double.Parse(response.RealtimeCurrencyExchangeRate.ExchangeRate, CultureInfo.InvariantCulture);
                var refreshTime = DateTime.Parse(response.RealtimeCurrencyExchangeRate.LastRefreshed);
                _logger.LogTrace($"Price update data successfully parsed for symbol: {currencyExchangeSymbol}. Price: {price}, RefreshTime: {refreshTime}");

                return new PriceUpdate(currencyExchangeSymbol, price, refreshTime);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Failed to deserialize Alpha Vantage response: {responseContent}");
                throw new PriceDataProviderException("Failed to deserialize response from Alpha Vantage.", ex);
            }
        }
    }

    // Helper class to deserialize the Alpha Vantage response
    public class AlphaVantageResponse
    {
        [JsonPropertyName("Realtime Currency Exchange Rate")]
        public RealtimeCurrencyExchangeRate RealtimeCurrencyExchangeRate { get; set; }
    }

    public class RealtimeCurrencyExchangeRate
    {
        [JsonPropertyName("5. Exchange Rate")]
        public string ExchangeRate { get; set; }

        [JsonPropertyName("6. Last Refreshed")]
        public string LastRefreshed { get; set; }
    }
}
