using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using Client.Service;
using Client.Service.Abstract;
using Client.Service.Sub;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects.Sockets;
using DynamicData;
using ProjectZeroLib.Enums;
using ProjectZeroLib.Utils;
using Order = Client.Service.Sub.Order;
using OrderStatus = Bybit.Net.Enums.OrderStatus;
using Position = Client.Service.Sub.Position;

namespace Client.Model.Burse
{
    public partial class BybitModel(SubscriptionsRepository subscriptions, BurseName name) : BurseModel(subscriptions, name)
    {
        private readonly BybitRestClient _rest = new(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = Bybit.Net.BybitEnvironment.DemoTrading;
            });
        private readonly BybitSocketClient _socket = new(options =>
            {
                options.OutputOriginalData = true;
                options.Environment = Bybit.Net.BybitEnvironment.DemoTrading;
            });

        #region Настройка подписок

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

            if (balance.Success && account.Success)
                result = true;

            return result;
        }
        protected override async Task SetupSocket()
        {
            var orders = await _socket.V5PrivateApi.SubscribeToOrderUpdatesAsync(async (update) =>
            {
                if (update != null)
                    await OnOrderUpdated(update);
            });
        }

        /// <summary>
        /// TODO: для ордеров, закрывающих позиции обновить расчет профита (data.UnrealizedPnl - ?).
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        private async Task OnOrderUpdated(DataEvent<IEnumerable<BybitOrderUpdate>> update)
        {
            foreach (var data in update.Data)
            {
                if (data.ClientOrderId == null) continue;

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
                if (position != null && data.Price != null)
                {
                    CalcProfit(position, (decimal)data.Price);
                    await UiInvoker.UiInvoke(() => sub.Positions.Remove(position));
                }
            }
        }
        private static void ChangeOrderStatus(BybitOrderUpdate data, Order order, Subscription sub)
        {
            if (data.Status == OrderStatus.New)
            {
                order.Date = data.CreateTime.ToString(sub.DateFormat);

                if (order.Status == "Live" && data.Price != null)
                    order.Price = (decimal)data.Price;
                else
                {
                    order.Status = "Live";
                }
            }
            else if (data.Status == OrderStatus.Filled)
            {
                if (data.LastPriceOnCreated == null || data.ExecutedFee == null) return;

                order.Price = (decimal)data.LastPriceOnCreated;
                order.Date = data.UpdateTime.ToString(sub.DateFormat);
                order.Status = data.Status.ToString();

                sub.Profit -= (decimal)data.ExecutedFee;
            }
        }
        private void OnAccountUpdated(DataEvent<IEnumerable<BybitBalance>> update)
        {
            var data = update.Data.First();

            if (data != null && data.TotalAvailableBalance != null)
                Balance = (decimal)data.TotalAvailableBalance;
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
        /// <summary>
        /// TODO: доработать для разных типов тикеры.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        protected override async Task<decimal> GetTickerPrice(Order order, int pos)
        {
            decimal price = 0;
            bool success;

            do
            {
                var ticker = await _rest.V5Api.ExchangeData.GetLinearInverseTickersAsync(Category.Linear, order.Id);
                success = ticker.Success;
                if (success)
                {
                    var data = ticker.Data.List.First();
                    if (order.Side == Side.Sell)
                    {
                        if (data.BestAskPrice != null)
                            price = (decimal)data.BestAskPrice;
                    }
                    else if (order.Side == Side.Buy)
                    {
                        if (data.BestBidPrice != null)
                            price = (decimal)data.BestBidPrice;
                    }
                }
            }
            while (!success);

            return price;
        }
        protected override async Task UpdateOrderPrice(Order order)
        {
            var category = order.Type == "Spot" ? Category.Linear : Category.Inverse;

            var edit = await _rest.V5Api.Trading.EditOrderAsync(
                category: category,
                symbol: order.Id, 
                clientOrderId: order.ClientOrderId,
                price: order.Price);

            if (edit.Error != null)
            {

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
            var category = order.Type == "Spot" ? Category.Linear : Category.Inverse;
            var quantity = category == Category.Inverse ? Math.Round(order.Size * order.Price) : order.Size;

            var trade = await _rest.V5Api.Trading.PlaceOrderAsync(
                category: category,
                symbol: order.Id,
                side: side,
                type: NewOrderType.Limit,
                quantity: quantity,
                price: order.Price,
                clientOrderId: order.ClientOrderId);

            if (trade.Error != null)
            {

            }
        }
        public async Task CloseOrder(Order order)
        {
            var trade = await _rest.V5Api.Trading.CancelOrderAsync(
                category: Category.Linear,
                symbol: order.Id,
                clientOrderId: order.ClientOrderId);
        }
        public async Task ClosePosition(Position position)
        {
            var side = position.Side == Side.Sell ? OrderSide.Buy : OrderSide.Sell;

            var trade = await _rest.V5Api.Trading.PlaceOrderAsync(
                category: Category.Linear,
                symbol: position.Id,
                side: side,
                type: NewOrderType.Market,
                quantity: position.Size,
                clientOrderId: position.ClientOrderId);
        }

        protected async Task SetLeverageOnStart(int leverage, string id)
        {
            await _rest.V5Api.Account.SetLeverageAsync(Category.Linear, id, leverage, leverage);
        }

        protected override void CalcProfit(Position position, decimal price)
        {
            position.Profit = (price - position.Price) * position.Size;
        }

        public override async void Test()
        {
            await Test001();
        }
        private async Task Test001()
        {
            var sub = Subscriptions.Items.FirstOrDefault(x => x.Code == "2001");

            var trade = await _rest.V5Api.Trading.PlaceOrderAsync(
                category: Category.Linear,
                symbol: "ETHUSDT", //"BTCUSDH25",
                side: OrderSide.Buy,
                type: NewOrderType.Limit,
                quantity: 0.01m,
                price: 2500,
                clientOrderId: "eth2");

            if (trade.Error != null)
            {

            }
        }
    }
}
