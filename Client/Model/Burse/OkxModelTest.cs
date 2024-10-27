using Client.Service;
using Client.Service.Abstract;
using Client.Service.Sub;
using DynamicData;
//using OKX.Api;
//using OKX.Api.Account;
//using OKX.Api.Common;
//using OKX.Api.Trade;
using OKX.Net.Enums;
using ProjectZeroLib;
using ProjectZeroLib.Enums;
using System.Globalization;

namespace Client.Model.Burse
{
    //public class OkxModelTest : BurseModel
    //{
    //    private readonly OkxRestApiOptions _restClientOptions;
    //    private readonly OkxRestApiClient _rest;
    //    private readonly OkxWebSocketApiOptions _socketClientOptions;
    //    private readonly OkxWebSocketApiClient _socket;

    //    public OkxModelTest(SubscriptionsService subscriptions, BurseName name) : base(subscriptions, name) 
    //    {
    //        _restClientOptions = new OkxRestApiOptions
    //        {
    //            DemoTradingService = true,
    //            RawResponse = true,
    //        };
    //        _rest = new OkxRestApiClient(_restClientOptions);
    //        _socketClientOptions = new OkxWebSocketApiOptions
    //        {
    //            DemoTradingService = true,
    //            RawResponse = true,
    //        };
    //        _socket = new OkxWebSocketApiClient(_socketClientOptions);
    //    }

    //    private void OnOrderUpdated(OkxTradeOrder order)
    //    {
    //        if (order != null)
    //        {
    //            if (order.ClientOrderId != null)
    //            {
    //                var sub = Subscriptions.Items.FirstOrDefault(x => order.ClientOrderId.Contains(x.ClientOrderId));
    //                if (sub != null)
    //                {
    //                    if (order.OrderState.Equals(OrderStatus.Live))
    //                    {
    //                        var _ = sub.Orders.Items.FirstOrDefault(x => x.ClientOrderId.Equals(order.ClientOrderId));
    //                        if (_ != null)
    //                        {
    //                            Logger.UiInvoke(() =>
    //                            {
    //                                _.Date = order.CreateTime.ToString(dateFormat);
    //                                _.OrderId = order.OrderId;
    //                                _.Status = order.OrderState.ToString();
    //                                _.Size = order.Quantity.ToString();
    //                                _.Price = (decimal)order.Price;
    //                            });
    //                        }
    //                    }
    //                    else if (order.OrderState.Equals(OrderStatus.Filled))
    //                    {
    //                        var ord = sub.Orders.Items.FirstOrDefault(x => x.OrderId.Equals(order.OrderId));
    //                        if (ord != null)
    //                            Logger.UiInvoke(() =>
    //                            {
    //                                ord.Status = "Filled";
    //                                var position = sub.Positions.Items.FirstOrDefault(x => x.InstrumentId.Equals(order.InstrumentId));
    //                                if (position != null)
    //                                {

    //                                }
    //                                else
    //                                {
    //                                    sub?.Positions.Add(new Position()
    //                                    {
    //                                        TradeId = order.TradeId,
    //                                        InstrumentId = order.InstrumentId,
    //                                    });
    //                                }
    //                            });
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    private void OnAccountUpdated(OkxAccountBalance data)
    //    {
    //        if (data != null)
    //        {
    //            var balance = data.Details.Find(x => x.Currency.Equals("USDT"));
    //            if (balance != null)
    //                Balance = balance.AvailableBalance;
    //            else
    //                Balance = 0;
    //        }
    //    }
    //    private void OnPositionUpdated(OkxAccountPosition data)
    //    {
    //        if (data != null)
    //        {
    //            lock (data)
    //            {
    //                Logger.UiInvoke(() =>
    //                {
    //                    foreach (var sub in Subscriptions.Items)
    //                    {
    //                        var position = sub.Positions.Items.FirstOrDefault(x => x.InstrumentId.Equals(data.InstrumentId));
    //                        if (position != null)
    //                        {
    //                            if (data.Liabilities != null)
    //                            {
    //                                position.Quantity = (decimal)data.Liabilities;
    //                                if (data.Liabilities > 0)
    //                                    position.Side = "Buy";
    //                                else
    //                                {
    //                                    position.Side = "Sell";
    //                                    position.Quantity *= -1;
    //                                }

    //                                if (data.AveragePrice != null)
    //                                    position.Price = (decimal)data.AveragePrice;
    //                                if (data.InstrumentId != null)
    //                                    position.InstrumentId = data.InstrumentId;
    //                                if (data.UnrealizedProfitAndLoss != null)
    //                                    position.Profit = (decimal)data.UnrealizedProfitAndLoss;
    //                            }
    //                            else
    //                            {
    //                                sub.Positions.Remove(position);
    //                            }
    //                        }
    //                        else
    //                            continue;
    //                    }
    //                });
    //            }
    //        }
    //    }

    //    protected override async Task<bool> GetConnection()
    //    {
    //        var api = ConfigService.GetKey("Okx", "Api");
    //        var secret = ConfigService.GetKey("Okx", "Secret");
    //        var word = ConfigService.GetKey("Okx", "Word");
    //        _rest.SetApiCredentials(api, secret, word);
    //        _socket.SetApiCredentials(api, secret, word);

    //        var result = false;
    //        var ticker = await _rest.Public.GetTickerAsync("BTC-USDT");
    //        var balance = await _socket.Account.SubscribeToAccountUpdatesAsync(OnAccountUpdated);
    //        if (ticker.Success && balance.Success)
    //            result = true;
    //        return result;
    //    }
    //    protected override async Task SetupSocket()
    //    {
    //        var account = await _socket.Account.SubscribeToPositionUpdatesAsync(OnPositionUpdated, OkxInstrumentType.Any);
    //        var orders = await _socket.Trade.SubscribeToOrderUpdatesAsync(OnOrderUpdated, OkxInstrumentType.Any);
    //        await GetOrders();
    //    }
    //    protected override async Task GetOrders()
    //    {
    //        var orders = await _rest.Trade.GetOpenOrdersAsync();
    //        if (orders.Data != null)
    //        {
    //            foreach (var order in orders.Data)
    //            {
    //                if (order.ClientOrderId != null)
    //                {
    //                    var sub = Subscriptions.Items.FirstOrDefault(x => order.ClientOrderId.Contains(x.ClientOrderId));
    //                    if (sub != null)
    //                    {
    //                        sub.Orders.Add(new()
    //                        {
    //                            Date = order.UpdateTime.ToString(),
    //                            OrderId = order.OrderId,
    //                            InstrumentId = order.InstrumentId,
    //                            InstrumentType = order.InstrumentType.ToString(),
    //                            ClientOrderId = order.ClientOrderId,
    //                            Price = (decimal)order.Price,
    //                            Side = order.OrderSide.ToString(),
    //                            Status = order.OrderState.ToString()
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
    //                List<OkxTradeOrderPlaceRequest> _ = [];

    //                foreach (var order in orders.Items)
    //                {
    //                    var size = Math.Round(0.995m * limit / (orders.Count * order.Price), 5);
    //                    order.Size = size.ToString(CultureInfo.InvariantCulture);
    //                }

    //                foreach (var order in orders.Items)
    //                {
    //                    OkxTradeOrderSide side = order.Side.Equals("Sell") ? OkxTradeOrderSide.Sell : OkxTradeOrderSide.Buy;
    //                    _.Add(new()
    //                    {
    //                        InstrumentId = order.InstrumentId,
    //                        TradeMode = OkxTradeMode.Cross,
    //                        OrderSide = side,
    //                        PositionSide = OkxTradePositionSide.Net,
    //                        OrderType = OkxTradeOrderType.LimitOrder,
    //                        Size = order.Size,
    //                        Price = order.Price.ToString(CultureInfo.InvariantCulture),
    //                        Currency = "USDT",
    //                        ClientOrderId = order.ClientOrderId,

    //                    });
    //                }
    //                var trade = await _rest.Trade.PlaceOrdersAsync(_);
    //            }
    //        }
    //    }
    //    protected override async void UpdateOrderByTime(Order order, StockInfo stock)
    //    {
    //        var ticker = await _rest.Public.GetTickerAsync(order.InstrumentId);
    //        if (ticker.Error != null) return;
    //        decimal price = 0;
    //        var mul = stock.PriceStep * stock.Position;

    //        if (order.Side.Equals("Sell"))
    //        {
    //            if (ticker.Data.AskPrice != null)
    //                price = (decimal)ticker.Data.AskPrice + mul;
    //        }
    //        else if (order.Side.Equals("Buy"))
    //        {
    //            if (ticker.Data.BidPrice != null)
    //                price = (decimal)ticker.Data.BidPrice - mul;
    //        }
    //        if (order.Price == price) return;
    //        order.Price = price;
    //        await _rest.Trade.AmendOrderAsync(order.InstrumentId, newPrice: order.Price, clientOrderId: order.ClientOrderId);
    //    }
    //    protected override async Task ClosePosition(Position position)
    //    {
    //        var close = await _rest.Trade.ClosePositionAsync(position.InstrumentId, OkxAccountMarginMode.Cross, currency: "USDT");
    //    }
    //    protected async void UpdateTradeLimit(decimal limit, string code)
    //    {
    //        var sub = Subscriptions.Items.FirstOrDefault(x => x.Code == code);
    //        if (sub != null)
    //        {
    //            decimal[] prices = new decimal[sub.Stocks.Count];
    //            for (int i = 0; i < sub.Stocks.Count; i++)
    //            {
    //                bool success;
    //                do
    //                {
    //                    var ticker = await _rest.Public.GetTickerAsync(sub.Stocks[i].InstrumentId);
    //                    success = ticker.Success;
    //                    if (success)
    //                    {
    //                        if (ticker.Data.AskPrice != null)
    //                            prices[i] = (decimal)ticker.Data.AskPrice * sub.Stocks[i].Limit * sub.Stocks[i].Equivalent;
    //                    }

    //                }
    //                while (!success);
    //            }
    //            decimal max = prices.Max();
    //            decimal updatedLimit = Math.Round(sub.TradeLimit / (max * sub.Stocks.Count), 1);
    //            if (updatedLimit == 0)
    //            {
    //                //
    //            }
    //            else
    //            {
    //                for (int j = 0; j < sub.Stocks.Count; j++)
    //                {
    //                    sub.Stocks[j].Limit *= sub.Stocks[j].Multiplier * updatedLimit;
    //                }
    //            }
    //        }
    //    }
    //    protected override bool CheckBalanceOnStart(decimal limit)
    //    {
    //        return Balance > limit;
    //    }
    //    protected async Task<bool> SetLeverageOnStart(int lever, string instId)
    //    {
    //        var leverage = await _rest.Account.SetLeverageAsync(lever, null, instId, OkxAccountMarginMode.Cross, OkxTradePositionSide.Net);
    //        while (leverage.Error != null) leverage = await _rest.Account.SetLeverageAsync(lever, null, instId, OkxAccountMarginMode.Cross, OkxTradePositionSide.Net);
    //        return true;
    //    }

    //    /// <summary>
    //    /// Выставление ордера по рынку в случае, если 
    //    /// заявка не исполнилась до получения нового сигнала.
    //    /// </summary>
    //    public async void ChangeOrderType(Order order)
    //    {
    //        var cansel = await _rest.Trade.CancelOrderAsync(order.InstrumentId, clientOrderId: order.ClientOrderId);
    //        if (cansel.Success)
    //        {
    //            OkxTradeMode tdMode = OkxTradeMode.Cross;
    //            OkxTradeOrderSide side = order.Side.Equals("Sell") ? OkxTradeOrderSide.Buy : OkxTradeOrderSide.Sell;
    //            var trade = await _rest.Trade.PlaceOrderAsync(order.InstrumentId, tdMode, side, OkxTradePositionSide.Net, OkxTradeOrderType.MarketOrder, decimal.Parse(order.Size), clientOrderId: order.ClientOrderId + 'm');
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
    //        var trade_01 = await _rest.Trade.PlaceOrderAsync("BTC-USDT", OkxTradeMode.Cross, OkxTradeOrderSide.Sell, OkxTradePositionSide.Net, OkxTradeOrderType.LimitOrder, 0.00001m, 68666, clientOrderId: "VitalUd00030", currency: "USDT");
    //    }
    //}
}
