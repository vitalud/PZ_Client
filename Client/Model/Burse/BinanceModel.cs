using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using Binance.Net.Objects.Models.Spot.Socket;
using Client.Service;
using Client.Service.Abstract;
using Client.Service.Sub;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects.Sockets;
using DynamicData;
using ProjectZeroLib;
using ProjectZeroLib.Enums;

namespace Client.Model.Burse
{
    public class BinanceModel : BurseModel
    {
        private readonly BinanceRestClient _rest;
        private readonly BinanceSocketClient _socket;

        private string _listenKey;

        public BinanceModel(SubscriptionsService subscriptions, BurseName name) : base(subscriptions, name)
        {
            _rest = new BinanceRestClient(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = Binance.Net.BinanceEnvironment.Testnet;
            });
            _socket = new BinanceSocketClient(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = Binance.Net.BinanceEnvironment.Testnet;
            });
        }

        protected override async Task<bool> GetConnection()
        {
            var api = ConfigService.GetKey("Binance", "Api");
            var secret = ConfigService.GetKey("Binance", "Secret");
            var credentials = new ApiCredentials(api, secret);
            _rest.SetApiCredentials(credentials);
            _socket.SetApiCredentials(credentials);

            var result = false;
            var ticker = await _rest.UsdFuturesApi.ExchangeData.GetTickerAsync("BTC-USDT");
            var listenKey = await _rest.UsdFuturesApi.Account.StartUserStreamAsync();
            _listenKey = listenKey.Data;
            var balance = await _socket.UsdFuturesApi.Account.SubscribeToUserDataUpdatesAsync(_listenKey, onAccountUpdate: OnAccountUpdated);
            if (ticker.Success && balance.Success)
            {
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
            var account = await _socket.SpotApi.Account.SubscribeToUserDataUpdatesAsync(_listenKey, onAccountPositionMessage: OnPositionUpdated, onOrderUpdateMessage: OnOrderUpdated);
        }

        private void OnOrderUpdated(DataEvent<BinanceStreamOrderUpdate> update)
        {
            if (update != null)
            {
                var data = update.Data;
                if (data.ClientOrderId != null)
                {
                    var sub = Subscriptions.Items.FirstOrDefault(x => data.ClientOrderId.Contains(x.ClientOrderId));
                    if (sub != null)
                    {
                        if (data.Status.Equals(OrderStatus.New))
                        {
                            var _ = sub.Orders.Items.FirstOrDefault(x => x.ClientOrderId.Equals(data.ClientOrderId));
                            if (_ != null)
                            {
                                Logger.UiInvoke(() =>
                                {
                                    _.Date = data.CreateTime.ToString(dateFormat);
                                    _.OrderId = data.Id.ToString();
                                    _.Status = data.Status.ToString();
                                    _.Size = data.Quantity;
                                    _.Price = data.Price;
                                });
                            }
                        }
                        else if (data.Status.Equals(OrderStatus.Filled))
                        {
                            var order = sub.Orders.Items.FirstOrDefault(x => x.OrderId.Equals(data.Id));
                            if (order != null)
                            {
                                Logger.UiInvoke(() =>
                                {
                                    order.Status = "Filled";
                                    var position = sub.Positions.Items.FirstOrDefault(x => x.InstrumentId.Equals(data.Symbol));
                                    if (position == null)
                                    {
                                        sub?.Positions.Add(new Position()
                                        {
                                            TradeId = order.TradeId,
                                            InstrumentId = order.InstrumentId,
                                        });
                                    }
                                });
                            }
                        }
                    }
                }
            }
        }
        private void OnAccountUpdated(DataEvent<BinanceFuturesStreamAccountUpdate> update)
        {
            if (update != null)
            {
                var balance = update.Data.UpdateData.Balances.FirstOrDefault(x => x.Asset.Equals("USDT"));
                if (balance != null)
                    Balance = balance.WalletBalance;
                else
                    Balance = 0;
            }
        }
        private void OnPositionUpdated(DataEvent<BinanceStreamPositionsUpdate> update)
        {
            if (update != null)
            {
                lock (update)
                {
                    Logger.UiInvoke(() =>
                    {
                        var data = update.Data;
                        foreach (var sub in Subscriptions.Items)
                        {
                            var position = sub.Positions.Items.FirstOrDefault(x => x.InstrumentId.Equals(update.Symbol));
                            if (position != null)
                            {
                                //if (data.Liabilities != null)
                                //{
                                //    position.Quantity = (decimal)data.Liabilities;
                                //    if (data.Liabilities > 0)
                                //        position.Side = "Buy";
                                //    else
                                //    {
                                //        position.Side = "Sell";
                                //        position.Quantity *= -1;
                                //    }

                                //    if (data.AveragePrice != null)
                                //        position.Price = (decimal)data.AveragePrice;
                                //    if (data.Symbol != null)
                                //        position.InstrumentId = data.Symbol;
                                //    if (data.UnrealizedProfitAndLoss != null)
                                //        position.Profit = (decimal)data.UnrealizedProfitAndLoss;
                                //}
                                //else
                                //{
                                //    sub.Positions.Remove(position);
                                //}
                            }
                            else
                                continue;
                        }

                    });
                }
            }
        }

        protected override async Task PlaceOrder(SourceList<Order> orders, decimal limit)
        {
            if (IsConnected)
            {
                if (Balance > limit)
                {
                    List<BinanceFuturesBatchOrder> _ = [];

                    foreach (var order in orders.Items)
                    {
                        order.Size = Math.Round(0.995m * limit / (orders.Count * order.Price), 5);
                    }

                    foreach (var order in orders.Items)
                    {
                        OrderSide side = order.Side.Equals("Sell") ? OrderSide.Sell : OrderSide.Buy;
                        _.Add(new()
                        {
                            Symbol = order.InstrumentId,
                            Type = FuturesOrderType.Limit,
                            Side = side,
                            //TradeMode = TradeMode.Cross,
                            PositionSide = PositionSide.Both,
                            Quantity = order.Size,
                            Price = order.Price,
                            //Asset = "USDT",
                            NewClientOrderId = order.ClientOrderId,

                        });
                    }
                    var trade = await _rest.UsdFuturesApi.Trading.PlaceMultipleOrdersAsync(_);
                }
            }
        }
        protected override async void UpdateOrderByTime(Order order, SubStockData stock, int position)
        {
            var ticker = await _rest.SpotApi.ExchangeData.GetTickerAsync(order.InstrumentId);
            if (ticker.Error != null) return;
            decimal price = 0;
            var mul = stock.PriceStep * stock.Position;

            OrderSide side = order.Side.Equals("Sell") ? OrderSide.Sell : OrderSide.Buy;

            if (side.Equals(OrderSide.Sell))
            {
                if (ticker.Data.BestAskPrice != null)
                    price = ticker.Data.BestAskPrice + mul;
            }
            else if (side.Equals(OrderSide.Buy))
            {
                if (ticker.Data.BestBidPrice != null)
                    price = ticker.Data.BestBidPrice - mul;
            }
            if (order.Price == price) return;
            order.Price = price;
            await _rest.SpotApi.Trading.CancelOrderAsync(order.InstrumentId, orderId: long.Parse(order.OrderId), newClientOrderId: order.ClientOrderId);
            await _rest.SpotApi.Trading.PlaceOrderAsync(order.InstrumentId, side, SpotOrderType.Limit, quantity: order.Size, price: order.Price, newClientOrderId: order.ClientOrderId);
        }
        protected override async Task ClosePosition(Position position, string clientOrderId)
        {
            //var close = await _rest.UsdFuturesApi.Trading.ClosePositionAsync(position.InstrumentId, MarginMode.Cross);
        }
        protected override bool CheckBalanceOnStart(decimal limit)
        {
            return Balance > limit;
        }
        protected async Task<bool> SetLeverageOnStart(int lever, string instId)
        {
            //var leverage = await _rest.UsdFuturesApi.Trading.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
            //while (leverage.Error != null) leverage = await _rest.UnifiedApi.Account.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
            return true;
        }

        /// <summary>
        /// Выставление ордера по рынку в случае, если 
        /// заявка не исполнилась до получения нового сигнала.
        /// </summary>
        public async void ChangeOrderType(Order order)
        {
            var cansel = await _rest.UsdFuturesApi.Trading.CancelOrderAsync(order.InstrumentId, orderId: long.Parse(order.OrderId));
            if (cansel.Success)
            {
                //TradeMode tdMode = TradeMode.Cross;
                OrderSide side = order.Side.Equals("Sell") ? OrderSide.Buy : OrderSide.Sell;
                var trade = await _rest.UsdFuturesApi.Trading.PlaceOrderAsync(order.InstrumentId, side, FuturesOrderType.Market, order.Size, positionSide: PositionSide.Both, newClientOrderId: order.ClientOrderId + 'm');
            }
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
            var trade = await _rest.UsdFuturesApi.Trading.PlaceOrderAsync("BTC-USDT", OrderSide.Sell, FuturesOrderType.Market, 0.00001m, price: 68666, positionSide: PositionSide.Both, newClientOrderId: "VitalUd00020");
        }
    }

}
