namespace StockBroadcaster.ApiService.Domain.PriceUpdateService
{
    public class PriceUpdate
    {
        public double Price { get; }

        public DateTime RefreshTime { get; }

        public string Instrument { get; }

        public PriceUpdate(string instrument, double price, DateTime refreshTime)
        {
            Instrument = instrument;
            Price = price;
            RefreshTime = refreshTime;
        }
    }
}
