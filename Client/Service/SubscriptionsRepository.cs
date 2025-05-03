using Client.Service.Sub;
using DynamicData;
using Newtonsoft.Json;
using ProjectZeroLib.Signal;
using ProjectZeroLib.Utils;
using ReactiveUI;
using Serilog;
using System.Reactive.Linq;

namespace Client.Service
{
    /// <summary>
    /// Хранилище подписок.
    /// </summary>
    public class SubscriptionsRepository
    {
        private readonly ILogger _logger;

        private readonly SourceList<Subscription> _subscriptions = new();
        public SourceList<Subscription> Subscriptions => _subscriptions;

        public SubscriptionsRepository(ILogger logger)
        {
            _logger = logger;
            _logger = logger.ForContext("ClassName", GetType().Name);

            var sub = new Subscription
            {
                Name = ProjectZeroLib.Enums.BurseName.Okx,
                Code = "0001",
                Instruments = "BTCUSDT / BTCUSDT_525252",
                Types = "CoinFutures / InverseFutures",
                Profit = 99999.99m,
                TradeLimit = 9999999
            };
            var order = new Order("BTC-USDT_525252", "horosh", ProjectZeroLib.Enums.BurseName.Noname, "asd", ProjectZeroLib.Enums.Side.Noname, 0.01m)
            {
                Date = DateTime.Now.ToString("dd/MM HH:mm:ss"),
                Size = 0.00001m,
                Price = 99999.99m,
                Status = "Canceled"
            };
            sub.Orders.Add(order);
            sub.Trades.Add(order);
            sub.Positions.Add(new Position("chel", "horosh", ProjectZeroLib.Enums.BurseName.Noname, 1, ProjectZeroLib.Enums.Side.Noname, "asd"));
            Subscriptions.Add(sub);
        }

        /// <summary>
        /// Добавляет полученную от сервера подписку в хранилище.
        /// </summary>
        /// <param name="data">Данные подписки.</param>
        /// <param name="onStatusChanged">Метод обработки статуса подписки.</param>
        public async Task GetSubscription(string data, Action<string, bool> onStatusChanged)
        {
            var sub = JsonConvert.DeserializeObject<Subscription>(data);

            if (sub == null) return;

            _logger.Debug($"Получена стратегия: {sub.Code}");
            ConfigService.GetInterval(sub);

            sub.ClientOrderId = $"st{sub.Code}";

            foreach (var stock in sub.Stocks)
            {
                sub.Instruments += stock.Id + '/';
                sub.Types += stock.Type + '/';
            }

            sub.Instruments = sub.Instruments.TrimEnd('/');
            sub.Types = sub.Types.TrimEnd('/');

            sub.WhenAnyValue(x => x.IsActive)
                .Skip(1)
                .Subscribe(_ => onStatusChanged(sub.Code, sub.IsActive));

            await UiInvoker.UiInvoke(() => Subscriptions.Add(sub));
        }

        /// <summary>
        /// Добавляет полученный от сервера сигнал в подписку.
        /// </summary>
        /// <param name="data">Данные сигнала.</param>
        public async Task GetSignal(string data)
        {
            var signal = JsonConvert.DeserializeObject<SignalData>(data);

            if (signal == null || signal.Signal == "ping") return;

            _logger.Debug($"Получен сигнал по стратегии: {signal.Code}");
            var sub = Subscriptions.Items.FirstOrDefault(x => x.Code == signal.Code);

            if (sub != null && sub.IsActive)
            {
                await sub.SignalHandler(signal);
            }
        }
    }
}
