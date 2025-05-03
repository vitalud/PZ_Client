using Client.Service;
using Client.Service.Abstract;
using CryptoExchange.Net.Objects.Sockets;
using OKX.Net.Clients;
using OKX.Net.Enums;
using OKX.Net.Objects;
using OKX.Net.Objects.Account;
using OKX.Net.Objects.Trade;
using ProjectZeroLib.Enums;
using Serilog;
using Order = Client.Service.Sub.Order;
using Position = Client.Service.Sub.Position;

namespace Client.Model.Crypto
{
    public partial class OkxModel(SubscriptionsRepository subscriptions, ILogger logger, BurseName name) : CryptoModel(subscriptions, logger, name)
    {
        private readonly OKXRestClient _rest = new(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = OKX.Net.OKXEnvironment.Demo;
            });
        private readonly OKXSocketClient _socket = new(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = OKX.Net.OKXEnvironment.Demo;
            });

        protected override async Task<bool> SubscribeToUpdates()
        {
            var api = ConfigService.GetKey("Okx", "Api");
            var secret = ConfigService.GetKey("Okx", "Secret");
            var word = ConfigService.GetKey("Okx", "Word");
            var credentials = new OKXApiCredentials(api, secret, word);

            _rest.SetApiCredentials(credentials);
            _socket.SetApiCredentials(credentials);

            var result = false;

            var ticker = await _rest.UnifiedApi.ExchangeData.GetTickerAsync("BTC-USDT");
            var balance = await _socket.UnifiedApi.Account.SubscribeToAccountUpdatesAsync(null, true, AccountUpdateHandler);
            var orders = await _socket.UnifiedApi.Trading.SubscribeToOrderUpdatesAsync(InstrumentType.Any, null, null, OrderUpdateHandler);
            if (ticker.Success && balance.Success && orders.Success)
                result = true;

            if (ticker.Error != null) _logger.Information($"[{Name}] Ошибка запроса ticker");
            if (balance.Error != null) _logger.Information($"[{Name}] Ошибка подписки AccountUpdates: код ошибки {balance.Error.Code}");
            if (orders.Error != null) _logger.Information($"[{Name}] Ошибка подписки OrderUpdates: код ошибки {orders.Error.Code}");

            return result;
        }

        private async void OrderUpdateHandler(DataEvent<OKXOrderUpdate> update)
        {
            var data = update.Data;

            if (data == null || data.ClientOrderId == string.Empty || data.ClientOrderId == null) return;

            var state = ParseOrderState(data.OrderState);
            if (state != OrderState.None)
            {
                var price = state == OrderState.Filled ? data.FillPrice : data.Price;
                var response = new OrderUpdateResponse(Name, data.UpdateTime, data.ClientOrderId, price ?? 0, state, data.Fee ?? 0, data.ProfitAndLoss ?? 0, Balance);

                await OnOrderUpdated(response);
            }
        }

        private void AccountUpdateHandler(DataEvent<OKXAccountBalance> update)
        {
            var data = update.Data;
            if (data == null) return;

            var balance = update.Data.Details.FirstOrDefault(x => x.Asset == "USDT");
            if (balance == null || balance.AvailableBalance == null) return;

            OnAccountUpdated((decimal)balance.AvailableBalance);
        }


        public override async Task PlaceOrder(Order order)
        {
            if (!IsConnected) return;

            var side = order.Side == Side.Sell ? OrderSide.Sell : OrderSide.Buy;
            var size = order.Size;

            if (order.Type == "Spot")
            {

            }
            else if (order.Type == "Futures")
            {

            }
            else if (order.Type == "Swap")
            {

            }

            var trade = await _rest.UnifiedApi.Trading.PlaceOrderAsync(
                symbol: order.Id,
                side: side,
                type: OrderType.Limit,
                quantity: order.Size,
                price: order.Price,
                positionSide: PositionSide.Net,
                tradeMode: TradeMode.Cross,
                asset: "USDT",
                clientOrderId: order.ClientOrderId);

            if (trade.Error != null)
            {

            }
        }
        public override async Task CloseOrder(Order order)
        {
            if (!IsConnected) return;

            var trade = await _rest.UnifiedApi.Trading.CancelOrderAsync(
                symbol: order.Id,
                clientOrderId: order.ClientOrderId);

            if (trade.Error != null)
            {

            }
        }
        public override async Task<decimal> GetTickerPrice(Order order, int pos)
        {
            decimal price = 0;
            bool success;

            do
            {
                var ticker = await _rest.UnifiedApi.ExchangeData.GetTickerAsync(order.Id);
                success = ticker.Success;
                if (success)
                {
                    if (order.Side == Side.Sell)
                    {
                        if (ticker.Data.BestAskPrice != null)
                            price = (decimal)ticker.Data.BestAskPrice;
                    }
                    else if (order.Side == Side.Buy)
                    {
                        if (ticker.Data.BestBidPrice != null)
                            price = (decimal)ticker.Data.BestBidPrice;
                    }
                }
            }
            while (!success);

            return price;
        }
        public override async Task UpdateOrderPrice(Order order)
        {
            await _rest.UnifiedApi.Trading.AmendOrderAsync(order.Id, newPrice: order.Price, clientOrderId: order.ClientOrderId);
        }
        public override async Task ClosePosition(Position position)
        {
            if (!IsConnected) return;

            var side = position.Side == Side.Sell ? OrderSide.Sell : OrderSide.Buy;

            var trade = await _rest.UnifiedApi.Trading.PlaceOrderAsync(
                symbol: position.Id,
                side: side,
                type: OrderType.Market,
                quantity: position.Size,
                positionSide: PositionSide.Net,
                tradeMode: TradeMode.Cross,
                asset: "USDT",
                clientOrderId: position.ClientOrderId);
        }
        public override async Task SetLeverage(string id, int leverage)
        {
            //var lever = await _rest.UnifiedApi.Account.SetLeverageAsync(leverage, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
            //while (lever.Error != null) lever = await _rest.UnifiedApi.Account.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
        }





        public static OrderState ParseOrderState(OrderStatus status)
        {
            if (status == OrderStatus.Live) return OrderState.Live;
            else if (status == OrderStatus.PartiallyFilled) return OrderState.PartiallyFilled;
            else if (status == OrderStatus.Filled) return OrderState.Filled;
            else if (status == OrderStatus.Canceled) return OrderState.Canceled;

            return OrderState.None;
        }

        public override async void Test()
        {
            await Test001();
        }

        private async Task Test001()
        {
            var time = DateTime.Now.Second;
            var side = OrderSide.Buy;
            if (time % 10 > 5)
            {
                side = OrderSide.Sell;
            }

            var trade_01 = await _rest.UnifiedApi.Trading.PlaceOrderAsync("BTC-USDT", side, OrderType.Market, 100m, tradeMode: TradeMode.Cross, positionSide: PositionSide.Net, asset: "USDT", clientOrderId: "st0001");
        }
    }
}
