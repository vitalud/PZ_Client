using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using Client.Service;
using Client.Service.Abstract;
using Client.Service.Sub;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects.Sockets;
using DynamicData;
using ProjectZeroLib;
using ProjectZeroLib.Enums;
using Order = Client.Service.Sub.Order;
using OrderStatus = Bybit.Net.Enums.V5.OrderStatus;
using Position = Client.Service.Sub.Position;

namespace Client.Model.Burse
{
    public class BybitModel : BurseModel
    {
        private readonly BybitRestClient _rest;
        private readonly BybitSocketClient _socket;

        public BybitModel(SubscriptionsService subscriptions, BurseName name) : base(subscriptions, name)
        {
            _rest = new BybitRestClient(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = Bybit.Net.BybitEnvironment.DemoTrading;
            });
            _socket = new BybitSocketClient(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = Bybit.Net.BybitEnvironment.DemoTrading;
            });
        }
        protected override async Task<bool> GetConnection()
        {
            var api = ConfigService.GetKey("Bybit", "Api");
            var secret = ConfigService.GetKey("Bybit", "Secret");
            var credentials = new ApiCredentials(api, secret);

            _rest.SetApiCredentials(credentials);
            _socket.SetApiCredentials(credentials);
            _rest.ClientOptions.ApiCredentials = credentials;
            _socket.ClientOptions.ApiCredentials = credentials;

            var result = false;
            var ticker = await _rest.V5Api.ExchangeData.GetSpotTickersAsync("BTCUSDT");
            var balance = await _rest.V5Api.Account.GetBalancesAsync(AccountType.Unified);
            if (balance.Success)
            {
                var data = balance.Data.List.First();
                if (data != null && data.TotalAvailableBalance != null)
                {
                    Balance = (decimal)data.TotalAvailableBalance;
                }
            }
            var account = await _socket.V5PrivateApi.SubscribeToWalletUpdatesAsync(update => 
            { 
                if (update != null) 
                    OnAccountUpdated(update); 
            });
            if (ticker.Success && account.Success)
                result = true;
            return result;
        }
        protected override async Task SetupSocket()
        {
            //var account = await _socket.V5PrivateApi.SubscribeToPositionUpdatesAsync(async (update) =>
            //{
            //    if (update != null)
            //        await OnPositionUpdated(update);
            //});
            var orders = await _socket.V5PrivateApi.SubscribeToOrderUpdatesAsync(async (update) =>
            {
                if (update != null)
                    await OnOrderUpdated(update);
            });
        }

        private async Task OnOrderUpdated(DataEvent<IEnumerable<BybitOrderUpdate>> update)
        {
            foreach (var data in update.Data)
            {
                if (data.ClientOrderId != null)
                {
                    var code = data.ClientOrderId.Split('_');
                    if (code.Length.Equals(3))
                    {
                        var sub = Subscriptions.Items.FirstOrDefault(x => x.Code.Equals(code[1]));
                        if (sub != null)
                        {
                            var order = sub.Orders.Items.FirstOrDefault(x => x.OrderId.Equals(data.OrderId));
                            if (order != null)
                            {
                                await Logger.UiInvoke(() => ChangeOrderStatus(data, order, sub));
                            }
                        }
                    }
                }
            }
        }
        private void ChangeOrderStatus(BybitOrderUpdate data, Order order, Subscription sub)
        {
            if (data.Status.Equals(OrderStatus.New))
            {
                order.Date = data.CreateTime.ToString(dateFormat);
                if (order.Status.Equals("Live"))
                {
                    order.Price = (decimal)data.Price;
                }
                else
                {
                    order.Status = "Live";
                }
            }
            else if (data.Status.Equals(OrderStatus.Filled))
            {
                order.Price = (decimal)data.LastPriceOnCreated;
                order.Date = data.UpdateTime.ToString(dateFormat);
                order.Status = data.Status.ToString();

                sub.Profit -= (decimal)data.ExecutedFee;

                var position = sub.Positions.Items.FirstOrDefault(x => x.InstrumentId.Equals(data.Symbol));
                if (position == null)
                {
                    sub.Positions.Add(new Position()
                    {
                        InstrumentId = order.InstrumentId,
                        Price = order.Price,
                        Side = order.Side,
                        Quantity = order.Size,
                        Ticks = data.UpdateTime.Ticks
                    });
                }
            }
            else if (data.Status.Equals(OrderStatus.Cancelled))
            {
                order.Date = data.UpdateTime.ToString(dateFormat);
                order.Status = data.Status.ToString();
            }
        }
        private void OnAccountUpdated(DataEvent<IEnumerable<BybitBalance>> update)
        {
            var data = update.Data.First();
            if (data != null && data.TotalAvailableBalance != null)
            {
                Balance = (decimal)data.TotalAvailableBalance;
            }
        }
        private async Task OnPositionUpdated(DataEvent<IEnumerable<BybitPositionUpdate>> update)
        {
            await Logger.UiInvoke(() =>
            {
                foreach (var data in update.Data)
                {
                    if (data.Side != null)
                    {
                        foreach (var sub in Subscriptions.Items)
                        {
                            var position = sub.Positions.Items.FirstOrDefault(x => x.InstrumentId.Equals(data.Symbol));
                            if (position != null)
                            {
                                if (data.Quantity != 0)
                                {
                                    position.Profit = (decimal)data.UnrealizedPnl;

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
                }
            });
        }
        protected override async Task PlaceOrder(SourceList<Order> orders, decimal limit)
        {
            if (IsConnected)
            {
                if (Balance > limit)
                {
                    try
                    {
                        foreach (var order in orders.Items)
                        {
                            var size = Math.Round(0.995m * limit / (orders.Count * order.Price), 3);
                            order.Size = size;
                            var side = order.Side.Equals("Sell") ? OrderSide.Sell : OrderSide.Buy;
                            var trade = await _rest.V5Api.Trading.PlaceOrderAsync(
                                category: Category.Linear,
                                symbol: order.InstrumentId,
                                side: side,
                                type: NewOrderType.Limit,
                                quantity: size,
                                price: order.Price,
                                clientOrderId: order.ClientOrderId + GetClientOrderId(32 - order.ClientOrderId.Length));

                            order.OrderId = trade.Data.OrderId;
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }
        protected override async void UpdateOrderByTime(Order order, SubStockData stock, int position)
        {
            var ticker = await _rest.V5Api.ExchangeData.GetLinearInverseTickersAsync(Category.Linear, order.InstrumentId);
            if (ticker.Error == null)
            {
                var data = ticker.Data.List.First();
                decimal price = 0;
                var mul = stock.PriceStep * position;

                if (order.Side.Equals("Sell"))
                {
                    if (data.BestAskPrice != null)
                        price = (decimal)data.BestAskPrice + mul;
                }
                else if (order.Side.Equals("Buy"))
                {
                    if (data.BestBidPrice != null)
                        price = (decimal)data.BestBidPrice - mul;
                }

                if (order.Price != price)
                    order.Price = price;

                await _rest.V5Api.Trading.EditOrderAsync(Category.Linear, order.InstrumentId, orderId: order.OrderId, price: order.Price);
            }
        }
        protected override async Task ClosePosition(Position position, string clientOrderId)
        {
            var side = position.Side.Equals("Buy") ? OrderSide.Sell : OrderSide.Buy;
            var close = await _rest.V5Api.Trading.PlaceOrderAsync(
                category: Category.Linear,
                symbol: position.InstrumentId,
                side: side,
                type: NewOrderType.Market,
            quantity: position.Quantity,
                clientOrderId: clientOrderId + GetClientOrderId(32 - clientOrderId.Length));
        }
        protected override bool CheckBalanceOnStart(decimal limit)
        {
            return Balance > limit;
        }
        protected async Task SetLeverageOnStart(int leverage, string id)
        {
            await _rest.V5Api.Account.SetLeverageAsync(Category.Linear, id, leverage, leverage);
        }

        /// <summary>
        /// Выставление ордера по рынку в случае, если 
        /// заявка не исполнилась до получения нового сигнала.
        /// </summary>
        //public async void ChangeOrderType(Order order)
        //{
        //    var cansel = await _rest.V5Api.Trading.CancelOrderAsync(Category.Undefined, order.InstrumentId, clientOrderId: order.ClientOrderId);
        //    if (cansel.Success)
        //    {
        //        var side = order.Side.Equals("Sell") ? OrderSide.Buy : OrderSide.Sell;
        //        var trade = await _rest.V5Api.Trading.PlaceOrderAsync(Category.Undefined, order.InstrumentId, side, NewOrderType.Market, order.Size, clientOrderId: order.ClientOrderId + 'm');
        //    }
        //}

        public override async void Test()
        {
            //await Test001(10);
            //await Test002();
            //await Test003();
            await Test004();
        }
        private async Task Test001(int num)
        {
            var flag = DateTime.Now.Second % 10;
            var side = flag > 5 ? "Sell" : "Buy";
            var ticker = await _rest.V5Api.ExchangeData.GetLinearInverseTickersAsync(Category.Linear, "BTCUSDT");
            if (ticker != null)
            {
                var sub = Subscriptions.Items.FirstOrDefault(x => x.Code.Equals("2003"));
                var orders = sub.Orders;
                orders.Add(new Order()
                {
                    ClientOrderId = "st_2003",
                    Date = DateTime.Now.ToString(dateFormat),
                    InstrumentId = "BTCUSDT",
                    InstrumentType = "Spot",
                    Price = ticker.Data.List.First().LastPrice + num,
                    Side = side,
                    Size = 0,
                    Status = "Waiting"
                });
            }
        }
        private async Task Test002()
        {
            var sub = Subscriptions.Items.FirstOrDefault(x => x.Code.Equals("2003"));
            UpdateOrderByTime(sub.Orders.Items[0], sub.Stocks[0], sub.Position);
        }
        private async Task Test003()
        {
            var sub = Subscriptions.Items.FirstOrDefault(x => x.Code.Equals("2003"));
            await ClosePosition(sub.Positions.Items[0], sub.ClientOrderId);
        }
        private async Task Test004()
        {
            var a = await _rest.V5Api.Trading.GetPositionsAsync(Category.Linear, "BTCUSDT");

        }
    }
}
