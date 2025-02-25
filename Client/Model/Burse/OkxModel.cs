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
using ProjectZeroLib.Enums;
using ProjectZeroLib.Utils;
using Order = Client.Service.Sub.Order;
using Position = Client.Service.Sub.Position;

namespace Client.Model.Burse
{
    public partial class OkxModel(SubscriptionsRepository subscriptions, BurseName name) : BurseModel(subscriptions, name)
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

        #region Настройка подписок

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
            var orders = await _socket.UnifiedApi.Trading.SubscribeToOrderUpdatesAsync(InstrumentType.Any, null, null, OnOrderUpdated);
        }

        private async void OnOrderUpdated(DataEvent<OKXOrderUpdate> update)
        {
            var data = update.Data;
            if (data == null || data.ClientOrderId == string.Empty || data.ClientOrderId == null) return;

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
        private static void ChangeOrderStatus(OKXOrderUpdate data, Order order, Subscription sub)
        {
            if (data.OrderState == OrderStatus.Live)
            {
                order.Date = data.CreateTime.ToString(sub.DateFormat);

                if (order.Status == "Live" && data.Price != null)
                    order.Price = (decimal)data.Price;
                else
                {
                    order.Status = "Live";
                }
            }
            else if (data.OrderState == OrderStatus.Filled)
            {
                if (data.FillPrice == null) return;

                order.Price = (decimal)data.FillPrice;
                order.Date = data.UpdateTime.ToString(sub.DateFormat);
                order.Status = data.OrderState.ToString();

                sub.Profit -= data.FillPnl;
            }
        }
        private void OnAccountUpdated(DataEvent<OKXAccountBalance> update)
        {
            var data = update.Data;
            if (data == null) return;

            var balance = update.Data.Details.FirstOrDefault(x => x.Asset == "USDT");
            if (balance == null || balance.AvailableBalance == null) return;

            Balance = (decimal)balance.AvailableBalance;
        }

        #endregion

        #region Реализация делегатов

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
        protected override async Task UpdateOrderPrice(Order order)
        {
            await _rest.UnifiedApi.Trading.AmendOrderAsync(order.Id, newPrice: order.Price, clientOrderId: order.ClientOrderId);
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
        public async Task CloseOrder(Order order)
        {
            var trade = await _rest.UnifiedApi.Trading.CancelOrderAsync(
                symbol: order.Id,
                clientOrderId: order.ClientOrderId);
        }
        public async Task ClosePosition(Position position)
        {
            var side = position.Side == Side.Sell ? OrderSide.Sell : OrderSide.Buy;

            var trade = await _rest.UnifiedApi.Trading.PlaceOrderAsync(
                symbol: position.Id,
                side: side,
                type: OrderType.Market,
                quantity: position.Size,
                price: position.Price,
                positionSide: PositionSide.Net,
                tradeMode: TradeMode.Cross,
                asset: "USDT",
                clientOrderId: position.ClientOrderId);
        }


        protected async Task<bool> SetLeverageOnStart(int lever, string instId)
        {
            var leverage = await _rest.UnifiedApi.Account.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
            while (leverage.Error != null) leverage = await _rest.UnifiedApi.Account.SetLeverageAsync(lever, MarginMode.Cross, symbol: instId, positionSide: PositionSide.Net);
            return true;
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
            //var trade_01 = await _rest.UnifiedApi.Trading.PlaceOrderAsync("BTC-USDT", OrderSide.Sell, OrderType.Limit, 0.00001m, tradeMode: TradeMode.Cross, positionSide: PositionSide.Net, price: 68666, clientOrderId: "VitalUd00030");
        }
    }
}
