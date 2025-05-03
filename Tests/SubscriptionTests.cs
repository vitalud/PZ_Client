using Client.Service;
using Client.Service.Sub;
using NUnit.Framework.Internal;
using ProjectZeroLib.Enums;
using Serilog;
using System.Diagnostics;
using System.Windows;

namespace Tests
{
    [TestFixture]
    public class SubscriptionTests
    {
        private readonly SubscriptionsRepository _subs = new(Log.Logger);
        public Subscription Sub => _subs.Subscriptions.Items[0];
        public Order Order => Sub.Orders.Items[0];
        public Order Trade => Sub.Trades.Items[Sub.Trades.Items.Count - 1];
        public Position Position => Sub.Positions.Items[0];

        [OneTimeSetUp]
        public void SetupAsync()
        {
            Application app = Application.Current ?? new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var data = "{\"Stocks\":[{\"Burse\":2,\"Id\":\"BTCUSDT\",\"Type\":\"Spot\",\"PriceStep\":0.1,\"Equivalent\":1,\"Round\":3}],\"Name\":2,\"Code\":\"2003\",\"Leverage\":1,\"ClientLimit\":25000}";
            //var data = "{\"Stocks\":[{\"Burse\":1,\"Id\":\"BTC-USDT\",\"Type\":\"Spot\",\"PriceStep\":0.1,\"Equivalent\":1,\"Round\":5},{\"Burse\":1,\"Id\":\"BTC-USDT-250328\",\"Type\":\"Futures\",\"PriceStep\":0.1,\"Equivalent\":0.01,\"Round\":1}],\"Name\":1,\"Code\":\"0001\",\"Leverage\":1,\"ClientLimit\":10000}";

            _subs.GetSubscription(data, OnSubscriptionStatusChanged);
            DispatcherUtil.DoEvents();

            Sub.IsActive = true;
            Sub.RegisterPlaceOrderHandler(PlaceOrder);
            Sub.RegisterCloseOrderHandler(CloseOrder);
            Sub.RegisterGetPositionalPriceHandler(GetTickerPrice);
            Sub.RegisterUpdateOrderPriceHandler(UpdateOrderPrice);
            Sub.RegisterClosePositionsHandler(ClosePosition);
            Sub.RegisterCheckBalanceHandler(CheckBalance);
        }

        private void OnSubscriptionStatusChanged(string code, bool status)
        {
            Debug.WriteLine($"status_{code}_{status}");
        }

        private async Task PlaceOrder(Order order)
        {
            await Task.Run(() => 
            {
                Debug.WriteLine("place orders");
                Order.Status = "Live";
            });
        }
        private async Task CloseOrder(Order order) {
            await Task.Run(() => Debug.WriteLine("close orders"));
        }
        private Task<decimal> GetTickerPrice(Order order, int pos)
        {
            return Task.FromResult(95000.25m);
        }
        private Task UpdateOrderPrice(Order order)
        {
            return Task.CompletedTask;
        }
        private async Task ClosePosition(Position position)
        {
            await Task.Run(() =>
            {
                Debug.WriteLine("close positions");
                Sub.Profit += 10;
            });
        }
        private bool CheckBalance(decimal limit)
        {
            return true;
        }

        private static void UseDispatcher()
        {
            for (int i = 0; i < 1000; i++)
            {
                DispatcherUtil.DoEvents();
            }
        }

        /// <summary>
        /// ¬ыставл€ет за€вку по первому сигналу стратегии.
        /// </summary>
        [Test]
        public void SignalHandler_Test1Async()
        {
            var sig = "{\"Code\":\"2003\",\"Signal\":\"Order -0,02%\",\"Percent\":0,\"Stocks\":[{\"Burse\":2,\"Id\":\"BTCUSDT\",\"Type\":\"Spot\",\"Side\":2}]}";

            _subs.GetSignal(sig);
            UseDispatcher();

            Assert.Multiple(() =>
            {
                Assert.That(Order.Status, Is.EqualTo("Live"));
                Assert.That(Order.Price, Is.Not.EqualTo(0));
                Assert.That(Order.Size, Is.Not.EqualTo(0));
                Assert.That(Order.Side, Is.EqualTo(Side.Sell));
            });
        }


        /// <summary>
        /// ѕереводит за€вку в сделки при оповещении об исполнении.
        /// </summary>
        [Test]
        public void SignalHandler_Test2Async()
        {
            var sig = "{\"Code\":\"2003\",\"Signal\":\"Order -0,02%\",\"Percent\":0,\"Stocks\":[{\"Burse\":2,\"Id\":\"BTCUSDT\",\"Type\":\"Spot\",\"Side\":2}]}";

            Order.Status = "Filled";
            UseDispatcher();

            Assert.Multiple(() =>
            {
                Assert.That(Sub.Trades.Items, Has.Count.EqualTo(1));

                Assert.That(Sub.Trades.Items, Has.Count.EqualTo(1));
                Assert.That(Trade.Status, Is.EqualTo("Filled"));

                Assert.That(Sub.Positions.Items, Has.Count.EqualTo(1));
            });
        }

        /// <summary>
        /// ќтбрасывает сигнал при не прохождении правил стратегии.
        /// </summary>
        [Test]
        public void SignalHandler_Test3Async()
        {
            var sig = "{\"Code\":\"2003\",\"Signal\":\"Order -0,02%\",\"Percent\":0,\"Stocks\":[{\"Burse\":2,\"Id\":\"BTCUSDT\",\"Type\":\"Spot\",\"Side\":2}]}";

            _subs.GetSignal(sig);
            UseDispatcher();

            Assert.Multiple(() =>
            {
                Assert.That(Sub.Orders.Items, Has.Count.EqualTo(0));

                Assert.That(Sub.Positions.Items, Has.Count.EqualTo(1));
            });
        }

        /// <summary>
        /// ¬ыставл€ет за€вку по сигналу, прошеднему правила стратегии.
        /// </summary>
        [Test]
        public void SignalHandler_Test4Async()
        {
            var sig = "{\"Code\":\"2003\",\"Signal\":\"Order -0,02%\",\"Percent\":0,\"Stocks\":[{\"Burse\":2,\"Id\":\"BTCUSDT\",\"Type\":\"Spot\",\"Side\":1}]}";

            _subs.GetSignal(sig);
            UseDispatcher();

            Assert.Multiple(() =>
            {
                Assert.That(Sub.Orders.Items, Has.Count.EqualTo(1));

                Assert.That(Sub.Positions.Items, Has.Count.EqualTo(0));

                Assert.That(Sub.Profit, Is.Not.EqualTo(0));
            });
        }
    }
}