using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using Client.Service;
using Client.Service.Abstract;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Objects.Sockets;
using ProjectZeroLib.Enums;
using Serilog;
using Order = Client.Service.Sub.Order;
using OrderStatus = Bybit.Net.Enums.OrderStatus;
using Position = Client.Service.Sub.Position;

namespace Client.Model.Crypto
{
    public partial class BybitModel(SubscriptionsRepository subscriptions, ILogger logger, BurseName name) : CryptoModel(subscriptions, logger, name)
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

        protected override async Task<bool> SubscribeToUpdates()
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
                    AccountUpdateHandler(update); 
            });

            var orders = await _socket.V5PrivateApi.SubscribeToOrderUpdatesAsync(async (update) =>
            {
                if (update != null)
                    await OrderUpdateHandler(update);
            });

            if (balance.Success && account.Success && orders.Success)
                result = true;

            if (balance.Error != null) _logger.Information($"[{Name}] Ошибка запроса Balances: код ошибки {balance.Error.Code}");
            if (account.Error != null) _logger.Information($"[{Name}] Ошибка подписки WalletUpdates: код ошибки {account.Error.Code}");
            if (orders.Error != null) _logger.Information($"[{Name}] Ошибка подписки OrderUpdates: код ошибки {orders.Error.Code}");

            return result;
        }

        private async Task OrderUpdateHandler(DataEvent<IEnumerable<BybitOrderUpdate>> update)
        {
            foreach (var data in update.Data)
            {
                if (data.ClientOrderId == null || data.Price == null) continue;

                var state = ParseOrderState(data.Status);
                if (state != OrderState.None)
                {
                    var price = state == OrderState.Filled ? data.LastPriceOnCreated : data.Price;
                    var response = new OrderUpdateResponse(Name, data.UpdateTime, data.ClientOrderId, price ?? 0, state, data.ExecutedFee ?? 0, data.ClosedPnl ?? 0, Balance);

                    await OnOrderUpdated(response);
                }
            }
        }

        private void AccountUpdateHandler(DataEvent<IEnumerable<BybitBalance>> update)
        {
            var data = update.Data.First();

            if (data == null || data.TotalAvailableBalance == null) return;

            OnAccountUpdated((decimal)data.TotalAvailableBalance);
        }

        public override async Task PlaceOrder(Order order)
        {
            if (!IsConnected) return;

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
        public override async Task CloseOrder(Order order)
        {
            if (!IsConnected) return;

            var trade = await _rest.V5Api.Trading.CancelOrderAsync(
                category: Category.Linear,
                symbol: order.Id,
                clientOrderId: order.ClientOrderId);
        }
        /// <summary>
        /// TODO: доработать для разных типов тикеры.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public override async Task<decimal> GetTickerPrice(Order order, int pos)
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
        public override async Task UpdateOrderPrice(Order order)
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
        public override async Task ClosePosition(Position position)
        {
            if (!IsConnected) return;

            var side = position.Side == Side.Sell ? OrderSide.Buy : OrderSide.Sell;

            var trade = await _rest.V5Api.Trading.PlaceOrderAsync(
                category: Category.Linear,
                symbol: position.Id,
                side: side,
                type: NewOrderType.Market,
                quantity: position.Size,
                clientOrderId: position.ClientOrderId);
        }
        public override async Task SetLeverage(string id, int leverage)
        {
            //await _rest.V5Api.Account.SetLeverageAsync(Category.Linear, id, leverage, leverage);
        }


        public static OrderState ParseOrderState(OrderStatus status)
        {
            if (status == OrderStatus.Active) return OrderState.Live;
            else if (status == OrderStatus.PartiallyFilled) return OrderState.PartiallyFilled;
            else if (status == OrderStatus.Filled) return OrderState.Filled;
            else if (status == OrderStatus.Cancelled) return OrderState.Canceled;

            return OrderState.None;
        }
        public override async void Test()
        {
            await Test001();
        }
        private async Task Test001()
        {
            var trade = await _rest.V5Api.Trading.PlaceOrderAsync(
                category: Category.Linear,
                symbol: "BTCUSDT",
                side: OrderSide.Sell,
                type: NewOrderType.Market,
                quantity: 0.01m,
                clientOrderId: "eth12");

            if (trade.Error != null)
            {

            }
        }
    }
}
