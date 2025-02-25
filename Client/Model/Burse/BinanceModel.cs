using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using Client.Service;
using Client.Service.Abstract;
using Client.Service.Sub;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects.Sockets;
using DynamicData;
using ProjectZeroLib.Enums;
using ProjectZeroLib.Utils;

namespace Client.Model.Burse
{
    public partial class BinanceModel(SubscriptionsRepository subscriptions, BurseName name) : BurseModel(subscriptions, name)
    {
        private readonly BinanceRestClient _rest = new(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = Binance.Net.BinanceEnvironment.Testnet;
            });
        private readonly BinanceSocketClient _socket = new(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = Binance.Net.BinanceEnvironment.Testnet;
            });

        private string _listenKey = string.Empty;

        #region Настройка подписок

        protected override async Task<bool> GetConnection()
        {
            var api = ConfigService.GetKey("Binance", "Api");
            var secret = ConfigService.GetKey("Binance", "Secret");
            var credentials = new ApiCredentials(api, secret);

            _rest.SetApiCredentials(credentials);
            _socket.SetApiCredentials(credentials);
            _rest.ClientOptions.ApiCredentials = credentials;
            _socket.ClientOptions.ApiCredentials = credentials;

            var listenKey = await _rest.UsdFuturesApi.Account.StartUserStreamAsync();
            _listenKey = listenKey.Data;

            var result = false;
            var balance = await _rest.UsdFuturesApi.Account.GetBalancesAsync();
            var updates = await _socket.UsdFuturesApi.Account.SubscribeToUserDataUpdatesAsync(_listenKey, onAccountUpdate: OnAccountUpdated);
            if (balance.Success && updates.Success)
            {
                foreach (var asset in balance.Data)
                {
                    if (asset.Asset == "USDT")
                    {
                        Balance = asset.AvailableBalance;
                        break;
                    }
                }

                result = true;
                _ = Task.Run(async () => {
                    while (true)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(30));
                        await _rest.UsdFuturesApi.Account.KeepAliveUserStreamAsync(listenKey.Data);
                    }
                });
            }
            return result;
        }
        protected override async Task SetupSocket()
        {
            var account = await _socket.UsdFuturesApi.Account.SubscribeToUserDataUpdatesAsync(_listenKey, onOrderUpdate: OnOrderUpdated);
        }

        private async void OnOrderUpdated(DataEvent<BinanceFuturesStreamOrderUpdate> update)
        {
            var data = update.Data.UpdateData;

            if (data == null || data.ClientOrderId == null) return;

            var type = data.ClientOrderId[..2];
            if (type != "st") return;

            var code = data.ClientOrderId.Substring(2, 4);

            var sub = Subscriptions.Items.FirstOrDefault(x => x.Code == code);
            if (sub == null) return;

            var order = sub.Orders.Items.FirstOrDefault(x => x.ClientOrderId == data.ClientOrderId);
            if (order != null)
            {
                await UiInvoker.UiInvoke(() => ChangeOrderStatus(data, order, sub));
                return;
            }

            var position = sub.Positions.Items.FirstOrDefault(x => x.ClientOrderId == data.ClientOrderId);
            if (position != null)
            {
                CalcProfit(position, data.Price);
                await UiInvoker.UiInvoke(() => sub.Positions.Remove(position));
            }
        }
        private static void ChangeOrderStatus(BinanceFuturesStreamOrderUpdateData data, Order order, Subscription sub)
        {
            if (data.Status == OrderStatus.New)
            {
                order.Date = data.UpdateTime.ToString(sub.DateFormat);

                if (order.Status == "Live")
                    order.Price = data.Price;
                else
                {
                    order.Status = "Live";
                }
            }
            else if (data.Status == OrderStatus.Filled)
            {
                order.Price = data.PriceLastFilledTrade;
                order.Date = data.UpdateTime.ToString(sub.DateFormat);
                order.Status = data.Status.ToString();

                sub.Profit -= data.Fee;
            }
        }
        private void OnAccountUpdated(DataEvent<BinanceFuturesStreamAccountUpdate> update)
        {
            if (update == null) return;

            var balance = update.Data.UpdateData.Balances.FirstOrDefault(x => x.Asset.Equals("USDT"));
            if (balance != null)
                Balance = balance.WalletBalance;
        }

        #endregion

        #region Реализации делегатов

        protected override async Task PlaceOrders(SourceList<Order> orders)
        {
            if (!IsConnected) return;

            var tasks = orders.Items.Select(PlaceOrder);

            await Task.WhenAll(tasks);
        }
        protected override async Task CloseOrders(SourceList<Order> orders)
        {
            if (!IsConnected) return;

            var tasks = orders.Items.Select(CloseOrder);

            await Task.WhenAll(tasks);
        }
        protected override async Task<decimal> GetTickerPrice(Order order, int pos)
        {
            decimal price = 0;
            bool success;

            do
            {
                var ticker = await _rest.UsdFuturesApi.ExchangeData.GetTickerAsync(order.Id);
                success = ticker.Success;
                if (success)
                {
                    price = ticker.Data.LastPrice;
                }
            }
            while (!success);

            return price;

        }
        protected override async Task UpdateOrderPrice(Order order)
        {
            var side = order.Side == Side.Sell ? OrderSide.Sell : OrderSide.Buy;

            var cancel = await _rest.UsdFuturesApi.Trading.CancelOrderAsync(
                symbol: order.Id,
                origClientOrderId: order.ClientOrderId);

            if (cancel.Error != null) return;
            else
            {
                var update = await _rest.UsdFuturesApi.Trading.PlaceOrderAsync(
                    symbol: order.Id,
                    side: side, 
                    type: FuturesOrderType.Limit, 
                    quantity: order.Size, 
                    price: order.Price, 
                    timeInForce: TimeInForce.GoodTillCanceled,
                    newClientOrderId: order.ClientOrderId);

                if (update.Error != null)
                {

                }
            }
        }
        protected override async Task ClosePositions(SourceList<Position> positions)
        {
            if (!IsConnected) return;

            var tasks = positions.Items.Select(ClosePosition);

            await Task.WhenAll(tasks);
        }

        #endregion

        public async Task PlaceOrder(Order order)
        {
            var side = order.Side == Side.Sell ? OrderSide.Sell : OrderSide.Buy;

            var trade = await _rest.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: order.Id,
                side: side,
                type: FuturesOrderType.Limit,
                quantity: order.Size,
                price: order.Price,
                positionSide: PositionSide.Both,
                timeInForce: TimeInForce.GoodTillCanceled,
                newClientOrderId: order.ClientOrderId);

            if (trade.Error != null)
            {

            }
        }
        public async Task CloseOrder(Order order)
        {
            var trade = await _rest.UsdFuturesApi.Trading.CancelOrderAsync(
                symbol: order.Id,
                origClientOrderId: order.ClientOrderId);
        }
        public async Task ClosePosition(Position position)
        {
            var side = position.Side == Side.Sell ? OrderSide.Buy : OrderSide.Sell;

            var trade = await _rest.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: position.Id,
                side: side,
                type: FuturesOrderType.Market,
                quantity: position.Size,
                price: position.Price,
                positionSide: PositionSide.Both,
                newClientOrderId: position.ClientOrderId);
        }

        protected async Task<bool> SetLeverageOnStart(int lever, string instId)
        {
            //var leverage = await _rest.UsdFuturesApi.Trading.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
            //while (leverage.Error != null) leverage = await _rest.UnifiedApi.Account.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
            return true;
        }
        protected override void CalcProfit(Position position, decimal price)
        {
             position.Profit = (price - position.Price) * position.Size;
        }

        public override async void Test()
        {
            await Test001();
            //var leverage = await _restClient.Account.SetLeverageAsync(3, null, "BTC-USDT", OkxMarginMode.Isolated, OkxPositionSide.Net);

            //var market_38 = await _restClient.Public.GetTickerAsync("BTC-USDT");
            //var market_39 = await _restClient.Public.GetOrderBookAsync("BTC-USDT", 40);
            //var tickerTime = market_38.Response.ResponseTime;
            //var orderBookTime = market_39.Response.ResponseTime;
            //var trade_01 = await _restClient.Trading.PlaceOrderAsync("BTC-USDT", OkxTradeMode.Cash, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.LimitOrder, 0.00001m, 68666, clientOrderId: "VitalUd00010");

            //var account_10 = await _restClient.Account.SetLeverageAsync(3, null, "BTC-USD-240628", OkxMarginMode.Cross, OkxPositionSide.Net);
            //var account_09 = await _restClient.Account.GetLeverageAsync("BTC-USD-240628", OkxMarginMode.Cross);
            //var trade_02 = await _restClient.Trading.PlaceOrderAsync("BTC-USDT-240628", OkxTradeMode.Cross, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.LimitOrder, 1, 67666, clientOrderId:"VitalUd00011");
            //var trade_02 = await _rest.Trading.PlaceOrderAsync("BTC-USDT", OkxTradeMode.Isolated, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.MarketOrder, 0.01m, currency: "USDT", clientOrderId:"VitalUd00033");
            //await GetOrders();

            //var leverage = await _restClient.Account.SetLeverageAsync(5, null, "BTC-USDT", OkxMarginMode.Isolated, OkxPositionSide.Net);
            //var trade_05 = await _rest.Trading.AmendOrderAsync("BTC-USDT", newPrice: 58666, clientOrderId: "VitalUd00033");

            //var sell = await _rest.Trading.PlaceOrderAsync("BTC-USDT", OkxTradeMode.Cross, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.MarketOrder, 0.01m, currency: "USDT", clientOrderId:"VitalUd00033");
            //Thread.Sleep(3000);
            //var buy = await _rest.Trading.PlaceOrderAsync("BTC-USDT", OkxTradeMode.Cross, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.MarketOrder, 0.01m, currency: "USDT", clientOrderId: "VitalUd00033");
        }

        private async Task Test001()
        {

        }
    }
}
