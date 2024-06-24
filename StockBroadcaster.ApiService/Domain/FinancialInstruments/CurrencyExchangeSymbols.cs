namespace StockBroadcaster.ApiService.Domain.FinancialInstruments
{
    public struct CurrencyExchangeSymbols
    {
        public static string EURUSD => "EUR-USD";
        public static string USDJPY => "USD-JPY";
        public static string BTCUSD => "BTC-USD";

        public static (string, string) Split(string currencyExchangeSymbol)
        {
            var currencies = currencyExchangeSymbol.Split('-');
            if (currencies.Length != 2)
            {
                throw new ArgumentException($"Argument '{currencyExchangeSymbol}' misformatted", nameof(currencyExchangeSymbol));
            }
            return (currencies[0], currencies[1]);
        }
    }
}
