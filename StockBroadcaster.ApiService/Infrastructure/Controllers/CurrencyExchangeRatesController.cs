using Microsoft.AspNetCore.Mvc;
using StockBroadcaster.ApiService.Domain.PriceUpdateService;

namespace StockBroadcaster.ApiService.Infrastructure.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrencyExchangeRatesController : ControllerBase
    {
        private readonly IPriceUpdateService _priceUpdateService;
        private readonly ILogger<CurrencyExchangeRatesController> _logger;

        public CurrencyExchangeRatesController(IPriceUpdateService priceUpdateService, ILogger<CurrencyExchangeRatesController> logger)
        {
            _priceUpdateService = priceUpdateService;
            _logger = logger;
        }

        [HttpGet("currencyExchangeRates")] // Updated route
        public IActionResult GetAvailableCurrencyExchangeRates() // Updated method name
        {
            var symbols = _priceUpdateService.GetAvailableCurrencyExchangeSymbols();
            return Ok(symbols);
        }

        [HttpGet("currencyExchangeRates/{currencyExchangeRate}/price")]
        public async Task<IActionResult> GetCurrentPriceUpdate(string currencyExchangeRate, CancellationToken cancellationToken)
        {
            try
            {
                var priceUpdate = await _priceUpdateService.GetCurrentPriceUpdateAsync(currencyExchangeRate, cancellationToken);
                if (priceUpdate == null)
                {
                    throw new FinancialInstrumentNotFoundException(currencyExchangeRate);
                }
                return Ok(priceUpdate.Price);
            }
            catch (FinancialInstrumentNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (PriceDataProviderUnavailableException)
            {
                return StatusCode(503, new { error = "Price data provider is currently unavailable." }); // 503 Service Unavailable
            }
            catch (PriceDataProviderException ex)
            {
                _logger.LogError(ex, "Error fetching price update for {Symbol}", currencyExchangeRate);
                return StatusCode(503, new { error = "Failed to fetch price update." }); // 503 Service Unavailable
            }
            // Catch other unexpected exceptions
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching price update for {Symbol}", currencyExchangeRate);
                return StatusCode(500, new { error = "An internal server error occurred." }); // 500 Internal Server Error
            }
        }
    }
}
