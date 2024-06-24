namespace StockBroadcaster.ApiService.Domain.StockBroadcastService
{
    public class StockUpdateData
    {
        public StockUpdateData(double price)
        {
            Price = price;
        }

        public double Price { get; set; }
    }
}
