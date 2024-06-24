namespace StockBroadcaster.ApiService.Domain.PriceUpdateService
{
    public class PriceDataProviderException : Exception
    {
        public PriceDataProviderException()
        {
        }

        public PriceDataProviderException(string message)
            : base(message)
        {
        }

        public PriceDataProviderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
