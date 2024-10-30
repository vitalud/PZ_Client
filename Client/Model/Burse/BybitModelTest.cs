using Bybit.Api;
using Client.Service;
using Client.Service.Abstract;
using Client.Service.Sub;
using DynamicData;
using ProjectZeroLib;
using ProjectZeroLib.Enums;
using System.Globalization;
using Order = Client.Service.Sub.Order;
using OrderStatus = Bybit.Net.Enums.V5.OrderStatus;
using Position = Client.Service.Sub.Position;

namespace Client.Model.Burse
{
    //public class BybitModelTest : BurseModel
    //{
    //    private readonly BybitRestApiClient _rest;
    //    private readonly BybitRestApiClientOptions _restOptions;
    //    private readonly BybitWebSocketClient _socket;
    //    private readonly BybitWebSocketClientOptions _socketOptions;

    //    public BybitModelTest(SubscriptionsService subscriptions, BurseName name) : base(subscriptions, name)
    //    {
    //        _restOptions = new BybitRestApiClientOptions();
    //        _socket = new BybitSocketClient(options =>
    //        {
    //            options.OutputOriginalData = true;
    //        });
    //    }
    //    protected override async Task<bool> GetConnection()
    //    {
    //        var api = ConfigService.GetKey("Bybit", "Api");
    //        var secret = ConfigService.GetKey("Bybit", "Secret");
    //        var credentials = new ApiCredentials(api, secret);
    //        _rest.SetApiCredentials(credentials);
    //        _socket.SetApiCredentials(credentials);

    //        var result = false;
    //        var ticker = await _rest.V5Api.ExchangeData.GetSpotTickersAsync("BTC-USDT");
    //        var balance = await _socket.V5PrivateApi.SubscribeToWalletUpdatesAsync(OnAccountUpdated);
    //        if (ticker.Success && balance.Success)
    //            result = true;
    //        return result;
    //    }
    //    protected override async Task SetupSocket()
    //    {
    //        var account = await _socket.V5PrivateApi.SubscribeToPositionUpdatesAsync(OnPositionUpdated);
    //        var orders = await _socket.V5PrivateApi.SubscribeToOrderUpdatesAsync(OnOrderUpdated);
    //    }

    //    private void OnOrderUpdated(DataEvent<IEnumerable<BybitOrderUpdate>> update)
    //    {
    //        if (update != null)
    //        {
    //            foreach (var data in  update.Data)
    //            {
    //                if (data.ClientOrderId != null)
    //                {
    //                    var sub = Subscriptions.Items.FirstOrDefault(x => data.ClientOrderId.Contains(x.ClientOrderId));
    //                    if (sub != null)
    //                    {
    //                        if (data.Status.Equals(OrderStatus.New))
    //                        {
    //                            var _ = sub.Orders.Items.FirstOrDefault(x => x.ClientOrderId.Equals(data.ClientOrderId));
    //                            if (_ != null)
    //                            {
    //                                Logger.UiInvoke(() =>
    //                                {
    //                                    _.Date = data.CreateTime.ToString(dateFormat);
    //                                    _.OrderId = long.Parse(data.OrderId);
    //                                    _.Status = data.Status.ToString();
    //                                    _.Size = data.Quantity.ToString();
    //                                    _.Price = (decimal)data.Price;
    //                                });
    //                            }
    //                        }
    //                        else if (data.Status.Equals(OrderStatus.Filled))
    //                        {
    //                            var order = sub.Orders.Items.FirstOrDefault(x => x.OrderId.Equals(data.OrderId));
    //                            if (order != null)
    //                            {
    //                                Logger.UiInvoke(() =>
    //                                {
    //                                    order.Status = "Filled";
    //                                    var position = sub.Positions.Items.FirstOrDefault(x => x.InstrumentId.Equals(data.Symbol));
    //                                    if (position == null)
    //                                    {
    //                                        sub?.Positions.Add(new Position()
    //                                        {
    //                                            TradeId = order.TradeId,
    //                                            InstrumentId = order.InstrumentId,
    //                                        });
    //                                    }
    //                                });
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    private void OnAccountUpdated(DataEvent<IEnumerable<BybitBalance>> update)
    //    {
    //        if (update != null)
    //        {
    //            foreach (var data in update.Data)
    //            {
    //                Balance = (decimal)data.TotalAvailableBalance;
    //            }
    //        }
    //    }
    //    private void OnPositionUpdated(DataEvent<IEnumerable<BybitPositionUpdate>> update)
    //    {
    //        if (update != null)
    //        {
    //            lock (update)
    //            {
    //                Logger.UiInvoke(() =>
    //                {
    //                    foreach (var data in update.Data)
    //                    {
    //                        foreach (var sub in Subscriptions.Items)
    //                        {
    //                            var position = sub.Positions.Items.FirstOrDefault(x => x.InstrumentId.Equals(data.Symbol));
    //                            if (position != null)
    //                            {
    //                                var a = data.Quantity;
    //                                if (data.Quantity != null)
    //                                {
    //                                    position.Quantity = (decimal)data.Quantity;
    //                                    if (data.Quantity > 0)
    //                                        position.Side = "Buy";
    //                                    else
    //                                    {
    //                                        position.Side = "Sell";
    //                                        position.Quantity *= -1;
    //                                    }

    //                                    if (data.AveragePrice != null)
    //                                        position.Price = (decimal)data.AveragePrice;
    //                                    if (data.Symbol != null)
    //                                        position.InstrumentId = data.Symbol;
    //                                    if (data.UnrealizedPnl != null)
    //                                        position.Profit = (decimal)data.UnrealizedPnl;
    //                                }
    //                                else
    //                                {
    //                                    sub.Positions.Remove(position);
    //                                }
    //                            }
    //                            else
    //                                continue;
    //                        }

    //                    }
    //                });
    //            }
    //        }
    //    }

    //    //вызывать метод при получении стратегии от сервера
    //    protected override async Task GetOrders()
    //    {
    //        var orders = await _rest.V5Api.Trading.GetOrdersAsync(Category.Undefined);
    //        if (orders.Data != null)
    //        {
    //            foreach (var order in orders.Data.List)
    //            {
    //                if (order.ClientOrderId != null)
    //                {
    //                    var sub = Subscriptions.Items.FirstOrDefault(x => order.ClientOrderId.Contains(x.ClientOrderId));
    //                    if (sub != null)
    //                    {
    //                        sub.Orders.Add(new()
    //                        {
    //                            Date = order.UpdateTime.ToString(),
    //                            OrderId = long.Parse(order.OrderId),
    //                            InstrumentId = order.Symbol,
    //                            //InstrumentType = order.InstrumentType.ToString(),
    //                            ClientOrderId = order.ClientOrderId,
    //                            Price = (decimal)order.Price,
    //                            Side = order.Side.ToString(),
    //                            Status = order.Status.ToString()
    //                        });
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    protected override async Task PlaceOrder(SourceList<Order> orders, decimal limit)
    //    {
    //        if (IsConnected)
    //        {
    //            if (Balance > limit)
    //            {
    //                List<BybitPlaceOrderRequest> _ = [];

    //                foreach (var order in orders.Items)
    //                {
    //                    var size = Math.Round(0.995m * limit / (orders.Count * order.Price), 5);
    //                    order.Size = size.ToString(CultureInfo.InvariantCulture);
    //                }

    //                foreach (var order in orders.Items)
    //                {
    //                    OrderSide side = order.Side.Equals("Sell") ? OrderSide.Sell : OrderSide.Buy;
    //                    _.Add(new()
    //                    {
    //                        Symbol = order.InstrumentId,
    //                        //TradeMode = TradeMode.Cross,
    //                        Side = side,
    //                        //PositionSide = PositionSide.Net,
    //                        OrderType = NewOrderType.Limit,
    //                        Quantity = decimal.Parse(order.Size),
    //                        Price = order.Price,
    //                        //Asset = "USDT",
    //                        ClientOrderId = order.ClientOrderId,

    //                    });
    //                }
    //                var trade = await _rest.V5Api.Trading.PlaceMultipleOrdersAsync(Category.Undefined, _);
    //            }
    //        }
    //    }
    //    protected override async void UpdateOrderByTime(Order order, StockInfo stock)
    //    {
    //        var _ = await _rest.V5Api.ExchangeData.GetSpotTickersAsync(order.InstrumentId);
    //        if (_.Error != null) return;
    //        decimal price = 0;
    //        var mul = stock.PriceStep * stock.Position;

    //        var ticker = _.Data.List.First();
    //        if (order.Side.Equals("Sell"))
    //        {
    //            if (ticker.BestAskPrice != null)
    //                price = (decimal)ticker.BestAskPrice + mul;
    //        }
    //        else if (order.Side.Equals("Buy"))
    //        {
    //            if (ticker.BestBidPrice != null)
    //                price = (decimal)ticker.BestBidPrice - mul;
    //        }
    //        if (order.Price == price) return;
    //        order.Price = price;
    //        //await _rest.V5Api.Trading.AmendOrderAsync(order.InstrumentId, newPrice: order.Price, clientOrderId: order.ClientOrderId);
    //    }
    //    protected override async Task ClosePosition(Position position)
    //    {
    //        //var close = await _rest.V5Api.Trading.ClosePositionAsync(position.InstrumentId, MarginMode.Cross);
    //    }
    //    protected override bool CheckBalanceOnStart(decimal limit)
    //    {
    //        return Balance > limit;
    //    }
    //    protected async Task<bool> SetLeverageOnStart(int lever, string instId)
    //    {
    //        //var leverage = await _rest.UnifiedApi.Account.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
    //        //while (leverage.Error != null) leverage = await _rest.UnifiedApi.Account.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
    //        return true;
    //    }

    //    /// <summary>
    //    /// Выставление ордера по рынку в случае, если 
    //    /// заявка не исполнилась до получения нового сигнала.
    //    /// </summary>
    //    public async void ChangeOrderType(Order order)
    //    {
    //        var cansel = await _rest.V5Api.Trading.CancelOrderAsync(Category.Undefined, order.InstrumentId, clientOrderId: order.ClientOrderId);
    //        if (cansel.Success)
    //        {
    //            OrderSide side = order.Side.Equals("Sell") ? OrderSide.Buy : OrderSide.Sell;
    //            var trade = await _rest.V5Api.Trading.PlaceOrderAsync(Category.Undefined, order.InstrumentId, side, NewOrderType.Market, decimal.Parse(order.Size), clientOrderId: order.ClientOrderId + 'm');
    //        }
    //    }

    //    public override async void Test()
    //    {
    //        await Test001();
    //        //var leverage = await _restClient.Account.SetLeverageAsync(3, null, "BTC-USDT", OkxMarginMode.Isolated, OkxPositionSide.Net);

    //        //var market_38 = await _restClient.Public.GetTickerAsync("BTC-USDT");
    //        //var market_39 = await _restClient.Public.GetOrderBookAsync("BTC-USDT", 40);
    //        //var tickerTime = market_38.Response.ResponseTime;
    //        //var orderBookTime = market_39.Response.ResponseTime;
    //        //var trade_01 = await _restClient.Trading.PlaceOrderAsync("BTC-USDT", OkxTradeMode.Cash, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.LimitOrder, 0.00001m, 68666, clientOrderId: "VitalUd00010");

    //        //var account_10 = await _restClient.Account.SetLeverageAsync(3, null, "BTC-USD-240628", OkxMarginMode.Cross, OkxPositionSide.Net);
    //        //var account_09 = await _restClient.Account.GetLeverageAsync("BTC-USD-240628", OkxMarginMode.Cross);
    //        //var trade_02 = await _restClient.Trading.PlaceOrderAsync("BTC-USDT-240628", OkxTradeMode.Cross, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.LimitOrder, 1, 67666, clientOrderId:"VitalUd00011");
    //        //var trade_02 = await _rest.Trading.PlaceOrderAsync("BTC-USDT", OkxTradeMode.Isolated, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.MarketOrder, 0.01m, currency: "USDT", clientOrderId:"VitalUd00033");
    //        //await GetOrders();

    //        //var leverage = await _restClient.Account.SetLeverageAsync(5, null, "BTC-USDT", OkxMarginMode.Isolated, OkxPositionSide.Net);
    //        //var trade_05 = await _rest.Trading.AmendOrderAsync("BTC-USDT", newPrice: 58666, clientOrderId: "VitalUd00033");

    //        //var sell = await _rest.Trading.PlaceOrderAsync("BTC-USDT", OkxTradeMode.Cross, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.MarketOrder, 0.01m, currency: "USDT", clientOrderId:"VitalUd00033");
    //        //Thread.Sleep(3000);
    //        //var buy = await _rest.Trading.PlaceOrderAsync("BTC-USDT", OkxTradeMode.Cross, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.MarketOrder, 0.01m, currency: "USDT", clientOrderId: "VitalUd00033");
    //    }

    //    private async Task Test001()
    //    {
    //        var trade = await _rest.V5Api.Trading.PlaceOrderAsync(Category.Undefined, "BTC-USDT", OrderSide.Sell, NewOrderType.Market, 0.00001m, clientOrderId: "VitalUd00050");
    //    }
    //}
}
