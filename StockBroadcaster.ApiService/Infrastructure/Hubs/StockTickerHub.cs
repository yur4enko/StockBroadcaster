using Microsoft.AspNetCore.SignalR;
using StockBroadcaster.ApiService.Domain.StockBroadcastService;

namespace StockBroadcaster.ApiService.Infrastructure.Hubs
{
    public class StockTickerHub : Hub<IStockTickerHubClient>
    {
        private readonly IStockBroadcastService _stockBroadcastService;

        public StockTickerHub(IStockBroadcastService stockBroadcastService)
        {
            _stockBroadcastService = stockBroadcastService;
        }

        public async Task Subscribe(string currencyExchangeSymbol)
        {
            await _stockBroadcastService.SubscribeAsync(Context.ConnectionId, currencyExchangeSymbol);
            await Groups.AddToGroupAsync(Context.ConnectionId, currencyExchangeSymbol);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var currencyExchangeSymbol = await _stockBroadcastService.UnsubscribeAsync(Context.ConnectionId);

            // SignalR suggests not to use await with Groups.RemoveFromGroupAsync
            if (currencyExchangeSymbol != null)
            {
                _ = Groups.RemoveFromGroupAsync(Context.ConnectionId, currencyExchangeSymbol);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
