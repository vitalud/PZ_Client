using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using Client.Service;
using Client.Service.Abstract;
using Client.Service.Sub;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects.Sockets;
using ProjectZeroLib.Enums;
using Serilog;
using System.Security.Principal;

namespace Client.Model.Crypto
{
    public partial class BinanceModel(SubscriptionsRepository subscriptions, ILogger logger, BurseName name) : CryptoModel(subscriptions, logger, name)
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

        protected override async Task<bool> SubscribeToUpdates()
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
            var updates = await _socket.UsdFuturesApi.Account.SubscribeToUserDataUpdatesAsync(_listenKey, onAccountUpdate: AccountUpdateHandler, onOrderUpdate: OnOrderUpdated);
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

            if (balance.Error != null) _logger.Information($"[{Name}] Ошибка запроса Balances: код ошибки {balance.Error.Code}");
            if (updates.Error != null) _logger.Information($"[{Name}] Ошибка подписки OrderUpdates: код ошибки {updates.Error.Code}");

            return result;
        }

        private async void OnOrderUpdated(DataEvent<BinanceFuturesStreamOrderUpdate> update)
        {
            var data = update.Data.UpdateData;

            if (data == null || data.ClientOrderId == null) return;

            var state = ParseOrderState(data.Status);
            if (state != OrderState.None)
            {
                var price = state == OrderState.Filled ? data.PriceLastFilledTrade : data.Price;
                var response = new OrderUpdateResponse(Name, data.UpdateTime, data.ClientOrderId, price , state, data.Fee, data.RealizedProfit, Balance);

                _logger.Information($"[{Name}] Получено обновление ордера ");

                await OnOrderUpdated(response);
            }
        }
        private void AccountUpdateHandler(DataEvent<BinanceFuturesStreamAccountUpdate> update)
        {
            if (update == null) return;

            var balance = update.Data.UpdateData.Balances.FirstOrDefault(x => x.Asset.Equals("USDT"));
            if (balance == null) return;

            OnAccountUpdated(balance.WalletBalance);
        }


        public override async Task PlaceOrder(Order order)
        {
            if (!IsConnected) return;

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
        public override async Task CloseOrder(Order order)
        {
            if (!IsConnected) return;

            var trade = await _rest.UsdFuturesApi.Trading.CancelOrderAsync(
                symbol: order.Id,
                origClientOrderId: order.ClientOrderId);
        }
        public override async Task<decimal> GetTickerPrice(Order order, int pos)
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
        public override async Task UpdateOrderPrice(Order order)
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
        public override async Task ClosePosition(Position position)
        {
            if (!IsConnected) return;

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
        public override async Task SetLeverage(string id, int leverage)
        {
            //var leverage = await _rest.UsdFuturesApi.Trading.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
            //while (leverage.Error != null) leverage = await _rest.UnifiedApi.Account.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
        }

        private static OrderState ParseOrderState(OrderStatus status)
        {
            if (status == OrderStatus.New) return OrderState.Live;
            else if (status == OrderStatus.PartiallyFilled) return OrderState.PartiallyFilled;
            else if (status == OrderStatus.Filled) return OrderState.Filled;
            else if (status == OrderStatus.Canceled) return OrderState.Canceled;

            return OrderState.None;
        }

        public override async void Test() { }
    }
}
