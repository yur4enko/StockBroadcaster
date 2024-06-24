namespace StockBroadcaster.ApiService.Domain.PriceUpdateService
{
    public class FinancialInstrumentNotFoundException : Exception
    {
        public string Symbol { get; }

        public FinancialInstrumentNotFoundException(string symbol)
            : base($"Financial instrument with symbol '{symbol}' not found.")
        {
            Symbol = symbol;
        }
    }
}
