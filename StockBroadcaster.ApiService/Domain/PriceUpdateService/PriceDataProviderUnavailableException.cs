namespace StockBroadcaster.ApiService.Domain.PriceUpdateService
{
    public class PriceDataProviderUnavailableException : Exception
    {
        public PriceDataProviderUnavailableException()
            : base("Price data provider is currently unavailable.")
        {
        }

        public PriceDataProviderUnavailableException(string message)
            : base(message)
        {
        }

        public PriceDataProviderUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
