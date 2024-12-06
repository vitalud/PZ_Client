using Client.Service;
using Client.Service.Abstract;
using Client.Service.Sub;
using CryptoExchange.Net.Objects.Sockets;
using DynamicData;
using OKX.Net.Clients;
using OKX.Net.Enums;
using OKX.Net.Objects;
using OKX.Net.Objects.Account;
using OKX.Net.Objects.Trade;
using ProjectZeroLib;
using ProjectZeroLib.Enums;
using System.Globalization;
using Order = Client.Service.Sub.Order;
using Position = Client.Service.Sub.Position;

namespace Client.Model.Burse
{
    public class OkxModel : BurseModel
    {
        private readonly OKXRestClient _rest;
        private readonly OKXSocketClient _socket;

        public OkxModel(SubscriptionsService subscriptions, BurseName name) : base(subscriptions, name) 
        {
            _rest = new OKXRestClient(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = OKX.Net.OKXEnvironment.Demo;
            });
            _socket = new OKXSocketClient(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = OKX.Net.OKXEnvironment.Demo;
            });
        }
        protected override async Task<bool> GetConnection()
        {
            var api = ConfigService.GetKey("Okx", "Api");
            var secret = ConfigService.GetKey("Okx", "Secret");
            var word = ConfigService.GetKey("Okx", "Word");
            var credentials = new OKXApiCredentials(api, secret, word);
            _rest.SetApiCredentials(credentials);
            _socket.SetApiCredentials(credentials);

            var result = false;
            var ticker = await _rest.UnifiedApi.ExchangeData.GetTickerAsync("BTC-USDT");
            var balance = await _socket.UnifiedApi.Account.SubscribeToAccountUpdatesAsync(null, true, OnAccountUpdated);
            if (ticker.Success && balance.Success)
                result = true;
            return result;
        }
        protected override async Task SetupSocket()
        {
            var account = await _socket.UnifiedApi.Trading.SubscribeToPositionUpdatesAsync(InstrumentType.Any, null, null, true, OnPositionUpdated);
            var orders = await _socket.UnifiedApi.Trading.SubscribeToOrderUpdatesAsync(InstrumentType.Any, null, null, OnOrderUpdated);
        }

        private void OnOrderUpdated(DataEvent<OKXOrderUpdate> update)
        {
            if (update != null)
            {
                var data = update.Data;
                if (data.ClientOrderId != null)
                {
                    var sub = Subscriptions.Items.FirstOrDefault(x => data.ClientOrderId.Contains(x.ClientOrderId));
                    if (sub != null)
                    {
                        if (data.OrderState.Equals(OrderStatus.Live))
                        {
                            var _ = sub.Orders.Items.FirstOrDefault(x => x.ClientOrderId.Equals(data.ClientOrderId));
                            if (_ != null)
                            {
                                Logger.UiInvoke(() =>
                                {
                                    _.Date = data.CreateTime.ToString(dateFormat);
                                    _.OrderId = data.OrderId.ToString();
                                    _.Status = data.OrderState.ToString();
                                    _.Size = (decimal)data.Quantity;
                                    _.Price = (decimal)data.Price;
                                });
                            }
                        }
                        else if (data.OrderState.Equals(OrderStatus.Filled))
                        {
                            var order = sub.Orders.Items.FirstOrDefault(x => x.OrderId.Equals(data.OrderId));
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
        private void OnAccountUpdated(DataEvent<OKXAccountBalance> update)
        {
            if (update != null)
            {
                var balance = update.Data.Details.FirstOrDefault(x => x.Asset.Equals("USDT"));
                if (balance != null)
                    Balance = (decimal)balance.AvailableBalance;
                else
                    Balance = 0;
            }
        }
        private void OnPositionUpdated(DataEvent<IEnumerable<OKXPosition>> update)
        {
            if (update != null)
            {
                lock (update)
                {
                    Logger.UiInvoke(() =>
                    {
                        foreach (var data in update.Data)
                        {
                            foreach (var sub in Subscriptions.Items)
                            {
                                var position = sub.Positions.Items.FirstOrDefault(x => x.InstrumentId.Equals(data.Symbol));
                                if (position != null)
                                {
                                    if (data.Liabilities != null)
                                    {
                                        position.Quantity = (decimal)data.Liabilities;
                                        if (data.Liabilities > 0)
                                            position.Side = "Buy";
                                        else
                                        {
                                            position.Side = "Sell";
                                            position.Quantity *= -1;
                                        }

                                        if (data.AveragePrice != null)
                                            position.Price = (decimal)data.AveragePrice;
                                        if (data.Symbol != null)
                                            position.InstrumentId = data.Symbol;
                                        if (data.UnrealizedProfitAndLoss != null)
                                            position.Profit = (decimal)data.UnrealizedProfitAndLoss;
                                    }
                                    else
                                    {
                                        sub.Positions.Remove(position);
                                    }
                                }
                                else
                                    continue;
                            }

                        }
                    });
                }
            }
        }

        //вызывать метод при получении стратегии от сервера
        protected override async Task PlaceOrder(SourceList<Order> orders, decimal limit)
        {
            if (IsConnected)
            {
                if (Balance > limit)
                {
                    try
                    {
                        List<OKXOrderPlaceRequest> request = [];

                        foreach (var order in orders.Items)
                        {
                            order.Size = Math.Round(0.995m * limit / (orders.Count * order.Price), 5);
                        }

                        foreach (var order in orders.Items)
                        {
                            var side = order.Side.Equals("Sell") ? OrderSide.Sell : OrderSide.Buy;
                            request.Add(new()
                            {
                                Symbol = order.InstrumentId,
                                TradeMode = TradeMode.Cross,
                                OrderSide = side,
                                PositionSide = PositionSide.Net,
                                OrderType = OrderType.Limit,
                                Quantity = order.Size,
                                Price = order.Price,
                                Asset = "USDT",
                                ClientOrderId = order.ClientOrderId,
                            });
                        }
                        var trade = await _rest.UnifiedApi.Trading.PlaceMultipleOrdersAsync(request);
                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {
                        //
                    }
                }
            }
        }
        protected override async void UpdateOrderByTime(Order order, SubStockData stock, int position)
        {
            var ticker = await _rest.UnifiedApi.ExchangeData.GetTickerAsync(order.InstrumentId);
            if (ticker.Error != null) return;
            decimal price = 0;
            var mul = stock.PriceStep * stock.Position;

            if (order.Side.Equals("Sell"))
            {
                if (ticker.Data.BestAskPrice != null)
                    price = (decimal)ticker.Data.BestAskPrice + mul;
            }
            else if (order.Side.Equals("Buy"))
            {
                if (ticker.Data.BestBidPrice != null)
                    price = (decimal)ticker.Data.BestBidPrice - mul;
            }
            if (order.Price == price) return;
            order.Price = price;
            await _rest.UnifiedApi.Trading.AmendOrderAsync(order.InstrumentId, newPrice: order.Price, clientOrderId: order.ClientOrderId);
        }
        protected override async Task ClosePosition(Position position, string clientOrderId)
        {
            var close = await _rest.UnifiedApi.Trading.ClosePositionAsync(position.InstrumentId, MarginMode.Cross);
        }
        protected async void UpdateTradeLimit(decimal limit, string code)
        {
            //var sub = Subscriptions.Items.FirstOrDefault(x => x.Code == code);
            //if (sub != null)
            //{
            //    decimal[] prices = new decimal[sub.Stocks.Count];
            //    for (int i = 0; i < sub.Stocks.Count; i++)
            //    {
            //        bool success;
            //        do
            //        {
            //            var ticker = await _rest.Public.GetTickerAsync(sub.Stocks[i].InstrumentId);
            //            success = ticker.Success;
            //            if (success)
            //            {
            //                if (ticker.Data.AskPrice != null)
            //                    prices[i] = (decimal)ticker.Data.AskPrice * sub.Stocks[i].Limit * sub.Stocks[i].Equivalent;
            //            }

            //        }
            //        while (!success);
            //    }
            //    decimal max = prices.Max();
            //    decimal updatedLimit = Math.Round(sub.TradeLimit / (max * sub.Stocks.Count), 1);
            //    if (updatedLimit == 0)
            //    {
            //        //
            //    }
            //    else
            //    {
            //        for (int j = 0; j < sub.Stocks.Count; j++)
            //        {
            //            sub.Stocks[j].Limit *= sub.Stocks[j].Multiplier * updatedLimit;
            //        }
            //    }
            //}
        }
        protected override bool CheckBalanceOnStart(decimal limit)
        {
            return Balance > limit;
        }
        protected async Task<bool> SetLeverageOnStart(int lever, string instId)
        {
            var leverage = await _rest.UnifiedApi.Account.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
            while (leverage.Error != null) leverage = await _rest.UnifiedApi.Account.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
            return true;
        }

        /// <summary>
        /// Выставление ордера по рынку в случае, если 
        /// заявка не исполнилась до получения нового сигнала.
        /// </summary>
        public async void ChangeOrderType(Order order)
        {
            var cansel = await _rest.UnifiedApi.Trading.CancelOrderAsync(order.InstrumentId, clientOrderId: order.ClientOrderId);
            if (cansel.Success)
            {
                TradeMode tdMode = TradeMode.Cross;
                OrderSide side = order.Side.Equals("Sell") ? OrderSide.Buy : OrderSide.Sell;
                var trade = await _rest.UnifiedApi.Trading.PlaceOrderAsync(order.InstrumentId, side, OrderType.Market, order.Size, positionSide: PositionSide.Net, tradeMode: tdMode, clientOrderId: order.ClientOrderId + 'm');
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
            var trade_01 = await _rest.UnifiedApi.Trading.PlaceOrderAsync("BTC-USDT", OrderSide.Sell, OrderType.Limit, 0.00001m, tradeMode: TradeMode.Cross, positionSide: PositionSide.Net, price: 68666, clientOrderId: "VitalUd00030");
        }
    }
}
